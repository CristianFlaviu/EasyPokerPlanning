# Progress

> Living status doc. Any agent (human or AI) reads this after `CLAUDE.md` + `docs/domain-model.md` to know what exists, what's broken, and what's next. Update at the **end of each slice**.

Last updated: 2026-05-28

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

### Redis current-round live state slice
- Application: added `IRoomLiveStateStore` for current round load/save/clear behavior
- Domain: added no-event restore paths for rehydrating `Room.CurrentRound` and `Round` from live state
- Infrastructure: added Redis-backed current round storage, including vote snapshots, phase, title, and start time
- Infrastructure: `RoomRepository.GetByIdAsync` / `GetByIdWithHistoryAsync` now reattach Redis current round state after loading PostgreSQL room metadata
- Infrastructure: EF no longer maps `CurrentRound` onto `rooms`; completed round history remains persisted in PostgreSQL
- Application: start/vote/reveal/reset handlers save current round state to Redis; end-round persists history then clears Redis current round state
- Verification: `dotnet build backend/PokerPlanning.slnx` passes after stopping the old running API process.

### Redis connection presence cleanup slice
- Application: expanded `IRoomLiveStateStore` with connection tracking and removal methods
- Infrastructure: Redis now tracks connection-to-room, room connection sets, and participant connection sets
- Infrastructure: disconnect/reconnect now updates presence keys without clearing current round state, so page reloads preserve active rounds
- Api: `RoomHub.JoinRoomGroup`, `LeaveRoomGroup`, and disconnect handling now update Redis presence alongside SignalR group membership
- Frontend: existing room disconnect flow already invokes `LeaveRoomGroup`, so no client change was needed for explicit room exits
- Verification: `dotnet build backend/src/PokerPlanning.Api/PokerPlanning.Api.csproj -o .codex-run/build-api` passes

### LeaveRoom slice
- Domain: `Room.LeaveRoom(...)` removes non-owner participants from the active room, demotes them if needed, and removes their active vote
- Domain events: `ParticipantLeftEvent`
- Application: `Features/LeaveRoom/` command, validator, handler, and notification handler
- Api: `DELETE /rooms/{id}/participants/me`
- SignalR: typed `ParticipantLeft` event removes seats, moderator ids, and active votes from connected clients
- Frontend: non-owner users can leave from the room rail and are routed back to history
- Verification: `dotnet build backend/PokerPlanning.slnx`, `npm run build`, and full `./scripts/smoke-test.ps1 -SkipBuild` pass after resetting the local Aspire Postgres volume

### Runtime E2E smoke after DB reset
- Reset local Docker volume `pokerplanning.apphost-51b673af0d-postgres-server-data`
- Full browser smoke passed for create room, SignalR connect, start round, vote, reveal, end round, and history display
- Smoke artifacts: `.codex-run/smoke-20260516-094417`

### History after leave hardening
- Infrastructure: `ListByParticipantIdAsync` now includes rooms where the participant appears in completed-round vote history, even if they later leave the active participant list
- Verification: `dotnet build backend/PokerPlanning.slnx`, `npm run build`, and full `./scripts/smoke-test.ps1 -SkipBuild` pass

### Local API URL verification
- Verified AppHost + API serves the frontend's configured `http://localhost:5218` API base URL during smoke testing
- Removed stale runtime blocker about AppHost dynamic API ports

### Room stylesheet budget
- Moved room page presentation styles from `room.page.scss` into the global stylesheet under a dedicated room page section
- Kept `room.page.scss` as a small pointer file so Angular's per-component style budget remains effective
- Verification: `npm run build` passes with no component style budget warning

### EF migration baseline
- Added `InitialCreate` EF Core migration for the settled PostgreSQL schema
- Added Infrastructure design-time `PokerPlanningDbContextFactory` so migrations can be generated without running the API startup project
- Api dev startup now runs `Database.MigrateAsync()` instead of `EnsureCreated()`
- EF migration history is stored in the `poker` schema alongside the app tables
- Reset the local Aspire Postgres volume and verified a fresh AppHost startup applies `20260516080150_InitialCreate`
- Verification: `dotnet build backend/PokerPlanning.slnx` and full `./scripts/smoke-test.ps1 -SkipBuild` pass
- Smoke artifacts: `.codex-run/smoke-20260516-110634`

### AI test use case docs
- Added `docs/ai-test-use-cases/` with one focused Markdown prompt per major test flow
- Covered create room, shared-link join, password-protected join, round lifecycle, moderator permissions, observer role, leave room, reconnect/current-round persistence, history, and a compact full v1 smoke
- Each use case includes setup, steps, expected results, and required failure-report details
- Added a shared Browser testing guide and linked every UI use case to it so AI agents know to use the Browser plugin/skill for localhost feature testing

### AI UX review docs
- Added `docs/ai-ux-review/` with prompts for AI-led product, page, and flow review
- Added a shared review guide requiring Browser plugin/skill inspection of the live app before proposing UX/design changes
- Covered full-product review, lobby, room, history, and alternative v1 flow proposals

### UX polish: mobile layout + revealed-round clarity
- Frontend room view now shortens long room ids in the app bar, wraps narrow controls, and prevents room stage/start-round/table surfaces from forcing horizontal overflow on mobile
- Revealed results now distinguish consensus, most common estimate, no clear leader/ties, and no-vote states instead of labeling every leading card as consensus
- Added a revealed vote distribution display and clearer moderator/voter next-action cues
- History detail layout now shortens long room ids, wraps headers/actions, and avoids narrow viewport clipping
- Verification: `npm run build`, `./scripts/smoke-test.ps1 -SkipBuild`, and a 390px mobile rendered check for tied revealed results + history detail passed
- Smoke artifacts: `.codex-run/smoke-20260516-220140`; mobile artifacts: `.codex-run/mobile-ux-20260516-220252`

### Room navbar share dialog polish
- Removed the room id chip from the room app bar and hid the healthy SignalR `connected` state; the app bar now only shows connection status when reconnecting/offline
- Replaced immediate share-link copy with an invite dialog showing the room URL and a copy button
- Restyled the room app-bar Share and History actions so History reads as a proper navigation button
- Verification: `npm run build` and targeted rendered checks for desktop share dialog, copy confirmation, clean navbar text, and 390px mobile navbar overflow passed
- Artifacts: `.codex-run/share-dialog-20260516-221133`

### Round reveal/reconnect bug fixes
- Backend `GET /rooms/{id}` now returns the caller's own card during the Voting phase while keeping other participants' cards hidden until reveal
- Domain round lifecycle now rejects revealing or archiving empty rounds, preventing `0 votes` history entries
- Frontend moderator reveal actions are disabled until at least one vote exists, with matching action cue copy
- Updated `docs/domain-model.md` with the empty-round reveal invariant
- Verification: `dotnet build backend/src/PokerPlanning.Api/PokerPlanning.Api.csproj -o .codex-run/build-api` and `npm run build` pass; full solution build was blocked by a running `PokerPlanning.Api` process locking Debug DLLs

### Moderator participant removal slice
- Domain: `Room.RemoveParticipant(...)` lets owners/moderators remove non-owner participants and reuses the participant-left event path
- Application/Api: added `RemoveParticipant` command and `DELETE /rooms/{id}/participants/{participantId}`
- Realtime: `ParticipantLeft` now also removes the participant's tracked SignalR connections from the room group after broadcasting the update
- Frontend: moderators can open the participant menu and remove other non-owner participants; owner-only moderator promote/demote controls remain unchanged
- Verification: `dotnet build backend/PokerPlanning.slnx`, `npm run build`, and targeted API smoke for moderator removal including active-vote cleanup pass

### Cloudflare Pages preview CORS slice
- Diagnosed Angular `status 0 Unknown Error` on commit-preview URLs as browser CORS blocking: canonical `https://easypokerplanning.pages.dev` was allowed, preview origins like `https://4b4aa298.easypokerplanning.pages.dev` were not
- Api: CORS now combines exact `Cors:AllowedOrigins` entries with wildcard `Cors:AllowedWildcardOrigins` entries and enables ASP.NET Core wildcard subdomain matching
- Deployment docs: documented Cloudflare preview URLs and the wildcard CORS setting used for Pages previews
- Verification: production canonical Pages create-room smoke passed before the code change; local API build verifies the new CORS code compiles

### Lobby join-by-link fix
- Frontend: `Join by link` on the lobby now opens a focused dialog instead of routing back to `/` or adding a second form to the create-room panel
- Frontend: the dialog accepts either a full `/room/{id}` URL or a raw room id and navigates to the room page
- Verification: `npm run build` passes; rendered check confirmed the dialog opens and a pasted room URL navigates to `/room/{id}`

---

## In progress / blocked

No active blockers.

---

## Next (priority order)

1. **History detail polish** — show more useful completed-round detail after the first UX polish slice:
   - Display vote breakdown with participant names/cards, not only final estimate and vote count.
   - Consider completed/ended timestamp or duration if already available through the API; avoid schema changes unless intentionally scoped.

---

## Tech debt (deferred, list out so it doesn't get lost)

- **EFCore.Relational version pin** — Api has explicit `10.0.8` pin to resolve conflict with Aspire's transitive `10.0.7`. Drop when Aspire publishes newer compatible version.
- **LeaveRoom observer history semantics** — voters remain discoverable through completed-round votes after leaving; observers who leave without votes still need an explicit historical membership model if that matters.

---

## Known issues

None currently tracked.

---

## How to use this file

- **Reading order for any agent:** `CLAUDE.md` → `docs/domain-model.md` → this file → `backend/CLAUDE.md` or `frontend/CLAUDE.md` depending on work area
- **Update rule:** at the end of every slice (a closed PR / merged feature), move items from "Next" → "Done", add new tech debt discovered, refresh "Last updated" date
- **Slicing rule:** each "Next" item should be small enough that a fresh agent can complete it in one session without ambiguity. If too vague, split.
