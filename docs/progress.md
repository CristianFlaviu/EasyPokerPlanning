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

### JoinRoom + GetRoom slice
- Domain: `Room.AddParticipant(...)` supports new participants and same-participant rejoin/rename; raises `ParticipantJoinedEvent`
- Application: `Features/JoinRoom/` with password verification through `IPasswordHasher`; `Features/GetRoom/` returns room metadata + participants
- Infrastructure: `RoomRepository.GetByIdAsync` includes owned participants
- Api: `POST /rooms/{id}/join` and `GET /rooms/{id}`
- Frontend: `RoomApiService.getRoom(...)` / `joinRoom(...)`; room page loads room metadata and participant list
- Verification: `dotnet build backend/PokerPlanning.slnx` and `npm run build` pass

### SignalR backend plumbing
- Api: `RoomHub` mapped at `/hubs/rooms`; hub methods only join/leave room groups and validate anonymous participant id
- Api: strongly typed `IRoomClient` with first event `ParticipantJoined`
- Application: `IRoomNotifier` abstraction and `ParticipantJoinedEventHandler`
- Infrastructure: `PokerPlanningDbContext.SaveChangesAsync` now publishes aggregate domain events through MediatR after persistence and clears them afterward
- Api: `RoomNotifier` wraps `IHubContext<RoomHub, IRoomClient>` and broadcasts `ParticipantJoined` to the room group
- Verification: `dotnet build backend/PokerPlanning.slnx` passes

### Frontend SignalR client
- Added npm package `@microsoft/signalr`
- Frontend: `core/signalr/SignalRService` owns hub connection lifecycle, participant signals, connection state, reconnect group rejoin, and `ParticipantJoined` handling
- Frontend: room page connects to `/hubs/rooms`, joins/leaves the room group, seeds participants from `GET /rooms/{id}`, and updates live participant list from SignalR
- Verification: `npm run build` passes

### Round lifecycle slice
- Domain: added `Round`, `CompletedRound`, `RoundPhase`, round lifecycle methods on `Room`, and domain errors for active/missing/invalid round transitions
- Domain events: `RoundStartedEvent`, `VoteSubmittedEvent`, `VotesRevealedEvent`, `RoundResetEvent`, `RoundEndedEvent`
- Application: command slices for `StartRound`, `SubmitVote`, `RevealVotes`, `ResetRound`, `EndRound` with notification handlers that broadcast through `IRoomNotifier`
- Infrastructure: EF maps `CurrentRound` onto `rooms` and completed history to `completed_rounds`; vote snapshots are stored as JSON text through Infrastructure converters
- Api: endpoints for `POST /rooms/{id}/rounds`, `/round/vote`, `/round/reveal`, `/round/reset`, `/round/end`; `GET /rooms/{id}` includes current round state
- SignalR: typed client now supports round started, vote submitted, votes revealed, round reset, and round ended events
- Frontend: room page has a minimal voting surface with start round, fixed Fibonacci deck, voted indicators, reveal/reset/end owner controls, and live SignalR round state
- Verification: `dotnet build backend/PokerPlanning.slnx` and `npm run build` pass

### Frontend room flow hardening
- Shared room links now show a join form for browsers whose `participantId` is not in the room
- Join flow supports password-protected rooms and joins as `Voter`
- Voting UI shows the current user's own selected card before reveal while other votes remain hidden
- Revealed rounds can be ended with a selected final Fibonacci estimate
- Verification: `npm run build` passes

### History slice
- Application: `GetParticipantRooms` and `GetRoomHistory` queries
- Infrastructure: repository methods load room history and participant room summaries
- Api: `GET /rooms/history?participantId=...` and `GET /rooms/{id}/history`
- Frontend: lazy `/history` page lists rooms for the current participant and lets users inspect completed rounds
- Verification: `dotnet build backend/PokerPlanning.slnx` and `npm run build` pass

### Moderator and role management slice
- Domain: `Room.PromoteToModerator`, `DemoteModerator`, and `ChangeRole`
- Domain events: `ModeratorPromotedEvent`, `ModeratorDemotedEvent`, `ParticipantRoleChangedEvent`
- Application: command slices and notification handlers for promote/demote/change role
- Infrastructure: `ModeratorIds` is now persisted as JSON text on `rooms`
- Api: endpoints for promoting/demoting moderators and changing the caller's voter/observer role
- SignalR: typed events update moderator ids and participant roles in connected clients
- Frontend: owner can promote/demote participants; users can toggle their own voter/observer role; moderator ids now gate moderator UI alongside owner
- Verification: `dotnet build backend/PokerPlanning.slnx` and `npm run build` pass

### Frontend visual refresh
- Replaced the plum/violet Material and custom theme tokens with a cooler blue/cyan palette
- Refined lobby, room, and history page surfaces with lighter professional styling, sharper radii, blue-tinted borders/shadows, and reduced decorative gradients
- Improved lobby hero spacing, scaled up the fanned card panel, fixed fan-entry animation transform behavior, and added a slim lobby footer
- Cleaned visible UI copy punctuation and observer indicators
- Verification: `npm run build` passes with the existing `room.page.scss` style budget warning

### Footer polish
- Modernized the shared Angular app footer with a responsive glassy band, stronger brand block, highlight chips, and clearer navigation/focus states
- Verification: `npm run build` passes with the existing `room.page.scss` style budget warning; Angular dev server responds at `http://127.0.0.1:4200/`

---

## In progress / blocked

- **Runtime E2E is blocked by stale local Postgres schema.**
  - AppHost starts and the API was reachable during smoke testing.
  - `POST /rooms` returned 500 because the existing Aspire Postgres data volume still has the original `poker.rooms` table shape.
  - Confirmed on 2026-05-15 with `\d poker.rooms`: the table lacks the new round/moderator columns added after the initial CreateRoom slice.
  - Until migrations exist, local dev needs a database/volume reset before end-to-end verification.
- **API URL still needs a stable frontend story.**
  - Frontend `environment.ts` points to `http://localhost:5218`.
  - AppHost assigns a dynamic proxied API port, so manual local runs may require updating the frontend API URL or running the API directly with valid connection strings.
  - Longer-term options: add fallback development connection strings, add Angular to AppHost, or generate frontend environment from Aspire.

---

## Next (priority order)

1. **Redis live state store** — `IRoomLiveStateStore` (Application interface) + Redis impl (Infrastructure). Per backend CLAUDE.md: Postgres = metadata + history; Redis = live round + votes + presence. Aggregates reconstituted from both.
2. **Runtime E2E smoke test after dev DB reset** — run AppHost + Angular, verify create/join/start/vote/reveal/end/history in browser. Current compile checks pass, but full request flow is blocked by the stale local database schema.

---

## Tech debt (deferred, list out so it doesn't get lost)

- **EF migration baseline** — currently `db.Database.EnsureCreated()` in dev. Replace with first migration once schema settles (post round-lifecycle).
- **EFCore.Relational version pin** — Api has explicit `10.0.8` pin to resolve conflict with Aspire's transitive `10.0.7`. Drop when Aspire publishes newer compatible version.
- **`LeaveRoom`** — domain method/command not yet implemented. Add once presence/live-state behavior is clearer.
- **Runtime verification automation** — add a repeatable smoke script after migrations or a database reset workflow exists.

---

## Known issues

- Existing Aspire Postgres data volume is stale and causes `POST /rooms` to return 500 until reset or migrated.
- End-to-end browser flow remains unverified; backend and frontend compile clean, and command-line smoke reached the API but stopped on stale schema.

---

## How to use this file

- **Reading order for any agent:** `CLAUDE.md` → `docs/domain-model.md` → this file → `backend/CLAUDE.md` or `frontend/CLAUDE.md` depending on work area
- **Update rule:** at the end of every slice (a closed PR / merged feature), move items from "Next" → "Done", add new tech debt discovered, refresh "Last updated" date
- **Slicing rule:** each "Next" item should be small enough that a fresh agent can complete it in one session without ambiguity. If too vague, split.
