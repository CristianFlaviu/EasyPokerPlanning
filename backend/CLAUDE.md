# Backend Agent Instructions (.NET 10)

> Loaded when working inside `backend/`. Read the root `CLAUDE.md` and `docs/domain-model.md` first.

## Project structure
```
backend/src/
├── PokerPlanning.Domain/         # Pure C#, no dependencies on anything outside .NET BCL
├── PokerPlanning.Application/    # Depends on Domain only. MediatR, FluentValidation
├── PokerPlanning.Infrastructure/ # Depends on Application + Domain. EF Core, Redis, auth, email, blob storage
├── PokerPlanning.Api/            # Depends on Application + Infrastructure. Hosts endpoints + hub + auth
├── PokerPlanning.AppHost/        # .NET Aspire orchestration (separate, references Api)
└── PokerPlanning.ServiceDefaults/ # Shared Aspire wiring: telemetry, health checks, resilience
```

`PokerPlanning.Domain` references **nothing** from this solution. It compiles in isolation.

## Domain layer rules
- Entities are mutable but only via methods. No public setters on aggregate state.
- Value objects (`Card`, `PasswordHash`, `ParticipantId`, `RoomId`, `UserId`) are records or readonly structs.
- Domain logic lives on aggregate methods. Example:
  ```csharp
  public sealed class Room
  {
      public RoomId Id { get; }
      public RoundState State { get; private set; }
      // ...

      public Result SubmitVote(ParticipantId participantId, Card card)
      {
          if (State.Phase != RoundPhase.Voting)
              return Result.Failure(RoomErrors.VotingNotOpen);
          if (!Participants.Any(p => p.Id == participantId))
              return Result.Failure(RoomErrors.NotInRoom);
          State.RecordVote(participantId, card);
          AddDomainEvent(new VoteSubmittedEvent(Id, participantId));
          return Result.Success();
      }
  }
  ```
- Use the project's own `Result` / `Result<T>` type (`Domain/Common/Result.cs`, paired with `Domain/Common/Error.cs`) for expected failures. Do not add `ErrorOr` / `FluentResults` — the in-house type is the standard here. Exceptions only for truly exceptional cases.
- Domain events are raised by aggregates and dispatched after persistence (UoW pattern).

## Application layer rules
- One folder per feature: `Application/Features/{FeatureName}/`
- Each folder contains:
  - `{FeatureName}Command.cs` (or `Query.cs`) — the MediatR request record
  - `{FeatureName}Handler.cs` — the handler
  - `{FeatureName}Validator.cs` — FluentValidation rules
- Handlers are **thin**:
  ```csharp
  public async Task<Result<Guid>> Handle(SubmitVoteCommand cmd, CancellationToken ct)
  {
      var room = await _rooms.GetByIdAsync(cmd.RoomId, ct);
      if (room is null) return Result.Failure<Guid>(RoomErrors.NotFound);

      var result = room.SubmitVote(cmd.ParticipantId, cmd.Card);
      if (result.IsFailure) return Result.Failure<Guid>(result.Error);

      await _rooms.SaveAsync(room, ct);
      return Result.Success(room.Id.Value);
  }
  ```
- Repositories are interfaces *in Application*, implemented in Infrastructure.
- Validators check *shape*, not business rules. Business rules belong in the domain.
- Use MediatR `IPipelineBehavior` for cross-cutting concerns: logging, validation dispatch, transaction wrapping.

## Infrastructure layer rules
- EF Core configurations in `Infrastructure/Persistence/Configurations/`, one per aggregate.
- Repository implementations adapt EF to domain — they may map between persistence shapes and aggregates if needed.
- Redis access goes through interfaces defined in Application (`IRoomLiveStateStore`).
- The `PokerPlanningDbContext` lives here; Application never references it directly.
- Other adapters implementing Application abstractions: `Security/` (`BCryptPasswordHasher`), `Email/` (`GmailSmtpEmailSender` → `IEmailSender`), `Storage/` (`AzureBlobAvatarStorage` → `IAvatarStorage`), `Time/` (`SystemClock` → `IClock`).

## API layer rules
- **Minimal APIs**, organized with endpoint route groups, one file per feature group:
  ```
  Api/Endpoints/
  ├── RoomEndpoints.cs       // POST /rooms, GET /rooms/{id}, GET /rooms/history
  ├── HealthEndpoints.cs
  └── EndpointExtensions.cs  // MapAllEndpoints()
  ```
- Each endpoint is a one-liner that dispatches to MediatR and maps the `Result` to an HTTP response.
- Use typed results (`Results<Ok<T>, BadRequest<ProblemDetails>, NotFound>`).
- The SignalR hub (`Hubs/RoomHub.cs`) is treated as a presentation concern, same layer as endpoints.

## SignalR hub rules
- Hub methods do three things only: authenticate the caller, build a command, dispatch it.
- Hub **never** touches `DbContext`, repositories, or domain entities directly.
- Broadcasts to clients happen in **domain event handlers**, not in the hub method itself. Pattern:
  1. Hub dispatches `SubmitVoteCommand` via MediatR
  2. Handler executes domain logic, persists, raises `VoteSubmittedEvent`
  3. A `VoteSubmittedEventHandler : INotificationHandler<VoteSubmittedEvent>` in Application calls `IRoomNotifier` (interface)
  4. `RoomNotifier` (Api layer) wraps `IHubContext<RoomHub, IRoomClient>` and pushes to the group
- Use a strongly-typed client interface `IRoomClient` with methods like `VoteSubmitted(ParticipantId)`, `VotesRevealed(IReadOnlyList<RevealedVote>)`.

## MediatR conventions
- Commands return `Result<T>` or `Result`. Queries return `Result<T>`.
- Use `IPipelineBehavior` for: validation, logging, unit-of-work / transaction scope.
- Notification handlers (`INotificationHandler<T>`) for domain events fired after persistence.

## C# 14 / .NET 10 idioms to prefer
- Primary constructors for handlers and services: `public class Foo(IBar bar)`
- File-scoped namespaces
- `required` on DTO properties when a default doesn't make sense
- Collection expressions: `Card[] deck = [new(1), new(2), ...];`
- `field` keyword in properties where it reduces boilerplate
- Records for DTOs, commands, queries, value objects

## Persistence split (Postgres vs Redis)
- **Postgres** stores: rooms (metadata: name, ownerId, ownerUserId, passwordHash, createdAt, archivedAt), completed rounds (title, final estimate, vote breakdown JSON), participant identities for history, **users** (id, email, display name, avatar url, external logins), and **email login tokens** (magic-link auth). See `Persistence/Migrations/` for the current schema.
- **Redis** stores: live room state (current round phase, current votes, participant connections, presence). Keys: `room:{id}:state`, `room:{id}:participants`, `participant:{id}:connection`.
- On `Reveal` and `EndRound`, persist the round to Postgres and either clear Redis (round ended) or reset for next round.
- Repository pattern: `IRoomRepository` (Postgres) and `IRoomLiveStateStore` (Redis) are separate interfaces. Aggregates are *reconstituted* from both as needed.

## Auth model (anonymous participant overlaid with optional user account)
Two independent identity layers coexist — do not conflate them:

**1. Anonymous participant (per-browser, room-scoped)**
- A `participantId` is generated client-side per browser, stored in localStorage, sent on every hub connect and HTTP request.
- The room owner is whoever created it — their `participantId` is the `OwnerId` on the room.
- Room password is hashed (BCrypt, via `BCryptPasswordHasher`) and stored as `PasswordHash` on the Room aggregate. Join requires submitting the password if set.
- Moderator status is granted by the owner via `PromoteModerator` / removed via `DemoteModerator`; moderator participantIds live on the room.
- Room-action authorization happens in handlers ("is caller the owner or a moderator?").

**2. User account (cross-room, optional sign-in)**
- A `User` aggregate exists (email, display name, avatar). Sign-in is **cookie-based** (scheme `pp.auth`, HttpOnly, 30-day sliding) — set up in `Api/Program.cs`.
- Two login paths: **Google OAuth** (`SignInWithGoogle`, only wired if `Authentication:Google:ClientId/Secret` configured) and **email magic-link** (`RequestEmailLogin` → emailed token → `ConsumeEmailLoginToken`).
- `IUserContext` (impl `Api/Security/UserContext.cs`) reads the current user id from cookie claims; null when not signed in.
- When a signed-in user creates a room, their `UserId` is captured as `OwnerUserId` (in addition to the anonymous `OwnerId`).
- Profile management: `UpdateProfile`, `UploadAvatar` (stored via `IAvatarStorage` → Azure Blob).

No JWT bearer tokens — auth is the ASP.NET Core cookie scheme above. Avatars and email are external services behind Application interfaces (`IAvatarStorage`, `IEmailSender`).

## Aspire AppHost
- The `AppHost` project orchestrates: Postgres container, Redis container, the API project, and (optionally) the Angular dev server.
- Use `builder.AddPostgres("postgres").WithDataVolume()` and `builder.AddRedis("redis")`.
- Wire references with `builder.AddProject<Projects.PokerPlanning_Api>("api").WithReference(postgres).WithReference(redis)`.
- The dashboard URL prints on startup; treat it as the primary local dev observability tool.

## Things to verify against current docs
- Aspire APIs evolved through 9.x → 10. Verify resource builder method names before using.
- MediatR v12+ removed some sync overloads. All handlers are async.
- EF Core 10 made compiled models default for some scenarios — check before manually configuring.

## Forbidden patterns
- Repository methods returning `IQueryable<T>` — they return materialized aggregates only
- `DbContext` injected anywhere outside Infrastructure
- Business validation in FluentValidation validators (use them for shape only)
- Calling `_hubContext.Clients...` from a hub method (broadcast through event handlers)
- Catching exceptions to return generic 500s — let middleware handle unhandled exceptions; use `Result` for expected failures
