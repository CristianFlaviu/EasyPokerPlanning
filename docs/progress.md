# Progress

> Living status doc. Any agent (human or AI) reads this after `CLAUDE.md` + `docs/domain-model.md` to know what exists, what's broken, and what's next. Update at the **end of each slice**.

Last updated: 2026-05-15

---

## Done

### Scaffold (Week 1)
- Solution `backend/PokerPlanning.slnx` (.NET 10 `.slnx` format) with 6 projects: Domain, Application, Infrastructure, Api, AppHost, ServiceDefaults
- Project references match dependency rules in `backend/CLAUDE.md`
- NuGet: MediatR 14.1.0, FluentValidation 12.1.1, EF Core 10.0.8, Npgsql.EFCore 10.0.1, StackExchange.Redis 2.13.1, Aspire 13.3.2, BCrypt.Net-Next 4.2.0, Microsoft.AspNetCore.OpenApi 10.0.8, Scalar.AspNetCore 2.14.14
- Angular 21 app in `frontend/` — zoneless, standalone, SCSS, Vitest, Material 3 (Azure/Blue, typography, animations)
- Root `.gitignore` (.NET + Aspire + Node + Angular + IDE + OS)

### CreateRoom slice (Week 2)
- Domain primitives: `Result`/`Error`, `IDomainEvent`, `AggregateRoot`, `RoomId`, `ParticipantId`, `Card` (Fibonacci deck), `PasswordHash`, `Participant`, `Room` aggregate with `Create` factory + invariants, `RoomCreatedEvent`
- Application: `IRoomRepository`, `IPasswordHasher`, `IClock`, `ValidationBehavior` pipeline, `Features/CreateRoom/` (Command + Validator + Handler), `AddApplication()` DI
- Infrastructure: `PokerPlanningDbContext`, `RoomConfiguration` (owned Participants), `RoomRepository`, `BCryptPasswordHasher`, `SystemClock`, `AddInfrastructure()` DI
- Api: Aspire-wired DbContext + Redis client, `Endpoints/RoomEndpoints.cs` → `POST /rooms`, `Common/ResultExtensions` maps `Result` → ProblemDetails, OpenAPI (`/openapi/v1.json`) + Scalar UI (`/scalar/v1`), CORS for `localhost:4200`, dev-only `EnsureCreated`
- AppHost: Postgres (with pgAdmin + data volume) + db `pokerplanning` + Redis (with RedisInsight) + Api wired via `WithReference` + `WaitFor`
- Frontend: `domain/room.ts` types, `IdentityService` (localStorage participantId), `participant-id` + `error` HTTP interceptors, `environments/environment.ts` (`apiBaseUrl = http://localhost:5218`), `features/lobby/` (RoomApiService + LobbyPage reactive form), `features/room/` placeholder, lazy routes, `app.config.ts` with `provideHttpClient` + `provideAnimationsAsync`

---

## In progress / blocked

- **CreateRoom end-to-end not verified.** Frontend `POST /rooms` returns status 0 (browser blocked / connection refused). Likely cause: Api standalone run fails because Aspire connection strings `postgres`/`redis` only injected by AppHost. AppHost assigns proxied ports, not 5218.
  - **Unblock options:**
    1. Add fallback `ConnectionStrings:postgres` + `ConnectionStrings:redis` in `appsettings.Development.json` so Api boots standalone against a local docker-compose Postgres
    2. OR launch AppHost, read assigned Api port from Aspire dashboard, update `frontend/src/environments/environment.ts`
    3. OR add Angular dev server to AppHost as a resource so Aspire injects the API URL into the FE env

---

## Next (priority order)

1. **JoinRoom slice** — domain method `room.AddParticipant(participantId, displayName, password?)` with password verify; `POST /rooms/{id}/join`; raises `ParticipantJoinedEvent`. Reuses same pattern as CreateRoom.
2. **GetRoom query** — `GET /rooms/{id}` returns metadata + participants. Frontend room.page needs it on load.
3. **SignalR plumbing (smallest E2E real-time proof):**
   - `RoomHub` in Api (auth via `X-Participant-Id`, group = roomId, only `JoinRoomGroup`/`LeaveRoomGroup` — no business logic)
   - `IRoomNotifier` interface in Application, impl in Api wraps `IHubContext<RoomHub, IRoomClient>`
   - `INotificationHandler<ParticipantJoinedEvent>` dispatches to notifier after `SaveChanges`
   - Frontend `SignalRService` with signal-based API (`participants`, `connectionState`)
   - First real-time event: `ParticipantJoined`
4. **Round lifecycle (5 commands, biggest chunk):**
   - `StartRound` (owner/mod only)
   - `SubmitVote` (voter only, replaces on re-submit, only during `Voting` phase)
   - `RevealVotes`
   - `ResetCurrentRound`
   - `EndRound` (persist `CompletedRound` to Postgres, raise event)
5. **Redis live state store** — `IRoomLiveStateStore` (Application interface) + Redis impl (Infrastructure). Per backend CLAUDE.md: Postgres = metadata + history; Redis = live round + votes + presence. Aggregates reconstituted from both.
6. **Frontend room voting UI** — card grid, participant list, reveal/reset buttons (owner+mod gated), live updates via SignalR signals.
7. **History views** — `GET /rooms/history?participantId=`, `GET /rooms/{id}/history`.

---

## Tech debt (deferred, list out so it doesn't get lost)

- **`Room.ModeratorIds` persistence** — currently `Ignore`d in `RoomConfiguration`. Needs JSON column or join table once `PromoteToModerator` ships.
- **EF migration baseline** — currently `db.Database.EnsureCreated()` in dev. Replace with first migration once schema settles (post round-lifecycle).
- **Domain event dispatch** — events raised on aggregate (`RoomCreatedEvent`) but no dispatcher pumps them to MediatR notification handlers after `SaveChanges`. Add `IUnitOfWork` pipeline behavior or `DbContext.SaveChangesAsync` override.
- **EFCore.Relational version pin** — Api has explicit `10.0.8` pin to resolve conflict with Aspire's transitive `10.0.7`. Drop when Aspire publishes newer compatible version.
- **`PromoteToModerator` / `DemoteFromModerator` / `ChangeRole` / `LeaveRoom`** — domain methods not yet on `Room`. Add when round lifecycle is in place.

---

## Known issues

- Frontend POST /rooms returns status 0 (see "In progress" above)
- No end-to-end smoke test run yet — backend + frontend compile clean but full request flow unverified

---

## How to use this file

- **Reading order for any agent:** `CLAUDE.md` → `docs/domain-model.md` → this file → `backend/CLAUDE.md` or `frontend/CLAUDE.md` depending on work area
- **Update rule:** at the end of every slice (a closed PR / merged feature), move items from "Next" → "Done", add new tech debt discovered, refresh "Last updated" date
- **Slicing rule:** each "Next" item should be small enough that a fresh agent can complete it in one session without ambiguity. If too vague, split.
