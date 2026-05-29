# Security & Pre-Launch Review

> Backend-focused architecture/security review ahead of go-live. This is a portfolio/learning app with low-value data, so the priority is not "defend against dedicated hackers." The priority is preventing easy room disruption, preserving password-protected room expectations, avoiding embarrassing reliability bugs, and keeping the deploy/test story clean.

Last reviewed: 2026-05-29 (Codex backend review)

| # | Severity | Title | Status |
|---|----------|-------|--------|
| 1 | Critical | Client-asserted participant identity enables room takeover | fixed |
| 2 | Critical | Password-protected rooms are readable/joinable over non-join paths | fixed |
| 3 | High | Concurrent votes can overwrite each other in Redis | open |
| 4 | High | Postgres/Redis dual-write can resurrect ended rounds | open |
| 5 | Medium | History/user lookup queries load too much data into memory | open |
| 6 | Medium | Domain-event dispatch is fragile and request-path coupled | open |
| 7 | Medium | Cookie account endpoints have CSRF exposure | open |
| 8 | Low | Email magic-link endpoint has no rate limiting | accepted risk for v1 |
| 9 | Low | No room lifecycle end (`ArchivedAt` never set) | open |
| 10 | Low | Expired/consumed email tokens never purged | open |
| 11 | Low | Find-or-create user has no graceful race handling | open |
| 12 | Low | Avatar validation trusts client-set content type | open |
| 13 | Low | Forwarded headers trusted from any source | accepted with Fly-only assumption |
| 14 | Low | `MigrateAsync()` runs on every startup | accepted while single instance |
| 15 | Pre-launch hygiene | Frontend default spec is stale and failing | open |

---

## 1. Critical: Client-asserted participant identity enables room takeover

**Problem.** `participantId` is the security principal for every room action, but the server accepts it from client-controlled inputs: `X-Participant-Id`, query string, or request body. The server never proves that the caller owns that seat.

This is made worse because `GET /rooms/{id}` returns the owner and moderator participant IDs to any caller.

**Impact.**
- Anyone with a room link can read `OwnerId`, then send that ID as `X-Participant-Id` and call moderator/owner actions.
- A visitor can start, reveal, reset, end, promote/demote, or remove participants.
- A visitor can submit or replace votes as another participant if their participant ID is known.
- `ChangeRole` accepts the target participant ID as caller identity, so a client can flip another participant between Voter/Observer.

This is still worth fixing even if the app has no valuable data: it is the easiest way for a normal user or tester to accidentally or deliberately ruin a room.

**Evidence.**
- `ResolveParticipantId` trusts request input: [RoomEndpoints.cs:117](../backend/src/PokerPlanning.Api/Endpoints/RoomEndpoints.cs#L117)
- Moderator actions derive caller from request input: [RoomEndpoints.cs:239](../backend/src/PokerPlanning.Api/Endpoints/RoomEndpoints.cs#L239)
- Owner/moderator IDs are returned in the room response: [GetRoomHandler.cs:58](../backend/src/PokerPlanning.Application/Features/GetRoom/GetRoomHandler.cs#L58)
- Authorization checks only ID equality: [Room.cs:287](../backend/src/PokerPlanning.Domain/Rooms/Room.cs#L287)

**Proposed fix.**
1. After create/join, issue a server-signed per-room seat token, stored in an httpOnly cookie or returned as a bearer-style room token.
2. Derive the caller participant ID from that token, not from body/header values.
3. Remove `CallerParticipantId` / `ParticipantId` override fields from action request bodies.
4. Keep participant IDs as public UI identifiers only if they are no longer authorization credentials.
5. Add domain or handler enforcement for caller-only actions such as `ChangeRole`.

**Resolution (2026-05-29).** Implemented a stateless, server-signed per-room **seat token**
(HMAC-SHA256 over `roomId:participantId:issuedAt`, `IRoomAccessTokenService` /
`HmacRoomAccessTokenService`). Issued on create/join, returned in the response body.
- Every mutating room action and `ChangeRole`/`Leave`/`Remove`/`Promote`/`Demote` now derives
  the caller from the token (`X-Room-Token` header), never from body/header/query GUIDs. The
  `CallerParticipantId` / `ParticipantId` override fields were removed from action request bodies.
- `ChangeRole` is now strictly "me": the caller can only change their own role (token-derived id).
- `Room.AddParticipant` rejects any join that targets an existing seat unless the caller already
  holds that seat's valid token, closing the "join with a known participant id to mint its token"
  takeover vector.
- Participant IDs remain public UI identifiers but are no longer authorization credentials.

---

## 2. Critical: Password-protected rooms are readable/joinable over non-join paths

**Problem.** Password verification happens only in `JoinRoom`. Other paths expose room state or add realtime listeners without proving the caller has joined or knows the password.

**Impact.**
- `GET /rooms/{id}` returns room metadata, participants, owner/moderator IDs, and active round state.
- `GET /rooms/{id}/history` returns completed vote history.
- `RoomHub.JoinRoomGroup` adds any connection to the SignalR group for a room ID, so a non-joined caller can receive live room events.

This matters because "password-protected room" is a user-facing promise. Even for a low-risk portfolio app, that promise should hold.

**Evidence.**
- `GetRoom` has no password/membership check: [RoomEndpoints.cs:129](../backend/src/PokerPlanning.Api/Endpoints/RoomEndpoints.cs#L129)
- `GetRoomHistory` loads history by room ID only: [GetRoomHistoryHandler.cs:13](../backend/src/PokerPlanning.Application/Features/GetRoomHistory/GetRoomHistoryHandler.cs#L13)
- SignalR group join only checks that a participant ID exists syntactically: [RoomHub.cs:10](../backend/src/PokerPlanning.Api/Hubs/RoomHub.cs#L10)

**Proposed fix.**
1. Treat successful create/join as the only way to receive a room access token.
2. Require that token on `GET /rooms/{id}`, room history, mutations, and SignalR `JoinRoomGroup`.
3. For password-protected rooms, return only a minimal "password required" response until access is proven.

**Resolution (2026-05-29).** The seat token from #1 is the only way to read full room state.
- `GET /rooms/{id}` without a valid token returns a **minimal preview** only — room name and
  `isPasswordProtected`. Participants, owner/moderator ids, and round state are withheld
  (`GetRoomHandler` short-circuits on `!HasAccess`), so the join screen can render without leaking.
- `GET /rooms/{id}/history` now requires either a valid current seat token or the signed-in
  account being linked to the room, so returning users can review completed sessions without
  rejoining.
- `RoomHub.JoinRoomGroup` validates the token (via SignalR `accessTokenFactory` → `access_token`
  query / `X-Room-Token`) against the requested room and confirms the seat is still a current
  participant before adding the connection to the group.
- Frontend stores the token per room (`pp.roomToken.{id}`), attaches it via the
  `roomTokenInterceptor`, and only opens the live connection once a token is held.

---

## 3. High: Concurrent votes can overwrite each other in Redis

**Problem.** Current-round state is stored as one Redis JSON blob. Handlers load the blob, mutate an in-memory `Room`, then save the full blob back. Two participants voting at nearly the same time can both read the same old blob, then the last writer wins.

**Impact.** Votes can disappear under normal team usage. This is not a hacker problem; it is a real planning-poker workflow problem.

**Evidence.**
- `SubmitVoteHandler` loads room/current round, mutates, then saves current round: [SubmitVoteHandler.cs:23](../backend/src/PokerPlanning.Application/Features/SubmitVote/SubmitVoteHandler.cs#L23)
- Redis save overwrites the full JSON value: [RedisRoomLiveStateStore.cs:40](../backend/src/PokerPlanning.Infrastructure/LiveState/RedisRoomLiveStateStore.cs#L40)

**Proposed fix.** Store votes atomically, e.g. Redis hash per active round:

```text
room:{roomId}:round -> metadata
room:{roomId}:round:{roundId}:votes -> HSET participantId card
```

Alternatively add optimistic versioning/CAS around the JSON blob.

---

## 4. High: Postgres/Redis dual-write can resurrect ended rounds

**Problem.** `EndRound` saves completed history to Postgres, then clears Redis. If the Redis clear fails after the database commit, the stale live round remains. Future room loads overlay Redis state onto the Postgres room, making an ended round appear active again.

**Impact.** Ghost rounds and state divergence after partial failure.

**Evidence.**
- End-round ordering: [EndRoundHandler.cs:37](../backend/src/PokerPlanning.Application/Features/EndRound/EndRoundHandler.cs#L37)
- Redis current round is reattached on read: [RoomRepository.cs:20](../backend/src/PokerPlanning.Infrastructure/Persistence/RoomRepository.cs#L20)

**Proposed fix.**
- On read, ignore Redis current-round state when that round ID already exists in completed history.
- Make `ClearCurrentRoundAsync` idempotent and retryable.
- Longer term: use an outbox or a durable round-state version so Postgres remains the source of truth for lifecycle transitions.

---

## 5. Medium: History/user lookup queries load too much data into memory

**Problem.** Some repository methods materialize broad tables and filter in C#.

**Impact.** Fine for a tiny portfolio dataset, but it will become slow and memory-heavy as rooms/history/users accumulate.

**Evidence.**
- `ListByParticipantIdAsync` loads all rooms with participants and history: [RoomRepository.cs:39](../backend/src/PokerPlanning.Infrastructure/Persistence/RoomRepository.cs#L39)
- `GetByExternalLoginAsync` loads all users with logins: [UserRepository.cs:20](../backend/src/PokerPlanning.Infrastructure/Persistence/UserRepository.cs#L20)

**Proposed fix.** Push filters into SQL and add indexes for `rooms.owner_user_id`, participant IDs, participant user IDs, and external login provider/subject lookups.

---

## 6. Medium: Domain-event dispatch is fragile and request-path coupled

**Problem.** `SaveChangesAsync` publishes domain events after the database save and before clearing events. Notification handlers run synchronously in the request path. The profile-update handler already needs a fresh DI scope to avoid re-entrant event publishing, which is a sign this design is brittle.

**Impact.** Broadcast failures can happen after commit with no retry. Slow handlers add latency to user requests. Re-entrant saves are easy to get wrong.

**Evidence.** Domain events are collected, saved, published, then cleared in [PokerPlanningDbContext.cs:25](../backend/src/PokerPlanning.Infrastructure/Persistence/PokerPlanningDbContext.cs#L25).

**Proposed fix.** For v1, document this as an accepted simplification if broadcasts are best-effort. For a stronger architecture, persist an outbox row in the same transaction and dispatch events out-of-band.

---

## 7. Medium: Cookie account endpoints have CSRF exposure

**Problem.** The auth cookie is `SameSite=None; Secure`, CORS allows credentials, and avatar upload disables antiforgery. Mutating account endpoints rely on the cookie.

**Impact.** Account profile/avatar/logout can be triggered cross-site. Room actions are mostly unaffected because they currently use participant IDs, not the account cookie.

**Evidence.**
- Cookie config: [Program.cs:65](../backend/src/PokerPlanning.Api/Program.cs#L65)
- Avatar upload disables antiforgery: [AuthEndpoints.cs:50](../backend/src/PokerPlanning.Api/Endpoints/AuthEndpoints.cs#L50)

**Proposed fix.** Add antiforgery tokens or require and validate a custom same-origin request header for cookie-authenticated mutating endpoints.

---

## 8. Low: Email magic-link endpoint has no rate limiting

**Problem.** `POST /auth/email/request` is anonymous and sends mail through Gmail SMTP without throttling.

**Why this is downgraded.** The app has no valuable data and you are not optimizing for malicious internet-scale abuse. This is still worth tracking because it can burn Gmail quota or make local testing noisy, but it should not outrank the room identity/password issues.

**Evidence.** Anonymous endpoint: [AuthEndpoints.cs:40](../backend/src/PokerPlanning.Api/Endpoints/AuthEndpoints.cs#L40)

**Proposed fix.** Add simple ASP.NET Core rate limiting keyed by IP and target email. A small fixed window is enough for this app.

---

## 9. Low: No room lifecycle end (`ArchivedAt` never set)

**Problem.** `ArchivedAt` is mapped but never assigned. Owners cannot leave, and rooms accumulate indefinitely.

**Evidence.** `ArchivedAt` exists on `Room`, but no domain method sets it: [Room.cs:39](../backend/src/PokerPlanning.Domain/Rooms/Room.cs#L39)

**Proposed fix.** Add owner-only archive/close-room behavior later, or accept indefinite retention for the portfolio demo.

---

## 10. Low: Expired/consumed email tokens never purged

**Problem.** `email_login_tokens` rows are added and consumed, but never deleted.

**Evidence.** Repository exposes add/lookup/save only: [EmailLoginTokenRepository.cs](../backend/src/PokerPlanning.Infrastructure/Persistence/EmailLoginTokenRepository.cs)

**Proposed fix.** Add a small cleanup job for consumed or expired tokens.

---

## 11. Low: Find-or-create user has no graceful race handling

**Problem.** Concurrent first-time logins for the same email can both observe no user, both try to insert, and the unique email index throws for one caller.

**Evidence.** Find/create flow: [ConsumeEmailLoginTokenHandler.cs:34](../backend/src/PokerPlanning.Application/Features/ConsumeEmailLoginToken/ConsumeEmailLoginTokenHandler.cs#L34)

**Proposed fix.** Catch unique-constraint violations and re-fetch the existing user, or use an upsert.

---

## 12. Low: Avatar validation trusts client-set content type

**Problem.** Upload validation checks client-declared content type and length, then stores that content type on the public blob.

**Impact.** Low. Blob content is static, but mislabeled files can be accepted.

**Evidence.**
- Validator: [UploadAvatarValidator.cs](../backend/src/PokerPlanning.Application/Features/UploadAvatar/UploadAvatarValidator.cs)
- Blob content type is set from request input: [AzureBlobAvatarStorage.cs:28](../backend/src/PokerPlanning.Infrastructure/Storage/AzureBlobAvatarStorage.cs#L28)

**Proposed fix.** Sniff image magic bytes server-side and derive the content type from the file signature.

---

## 13. Low: Forwarded headers trusted from any source

**Problem.** `KnownProxies.Clear()` and `KnownIPNetworks.Clear()` accept forwarded headers from any source. This is acceptable if the API is reachable only through Fly's proxy.

**Evidence.** Forwarded header options: [Program.cs:40](../backend/src/PokerPlanning.Api/Program.cs#L40)

**Proposed fix.** Document the Fly-only assumption, or restrict known proxies/networks if Fly provides stable values.

---

## 14. Low: `MigrateAsync()` runs on every startup

**Problem.** Database migration runs unconditionally at boot.

**Impact.** Fine for one Fly machine. Risk appears only if the app scales to multiple instances or startup latency matters.

**Evidence.** Startup migration: [Program.cs:189](../backend/src/PokerPlanning.Api/Program.cs#L189)

**Proposed fix.** Keep as-is for v1 single-instance deployment, or move migration into a deploy step before scaling out.

---

## 15. Pre-launch hygiene: Frontend default spec is stale and failing

**Problem.** The app builds, but the existing Angular spec still expects the default generated title.

**Evidence.** Failing assertion: [app.spec.ts:21](../frontend/src/app/app.spec.ts#L21)

**Verification.** `npm test -- --watch=false` fails on `should render title`.

**Proposed fix.** Either update the spec to assert the real shell behavior or remove the stale generated test if frontend tests are not part of the current quality gate.

---

## What is already good

- Clean architecture boundaries are mostly respected: Domain has no project references; Application depends on Domain; Infrastructure adapts EF/Redis/email/blob storage; API stays thin.
- Domain logic mostly lives on `Room` / `Round`; handlers stay thin.
- SignalR hub has no business logic.
- Magic-link tokens use CSPRNG, SHA-256 at rest, one-time consumption, and a 15-minute lifetime.
- Unknown-email login requests return success, avoiding basic account enumeration.
- Isolated backend build passes cleanly: `dotnet build backend/src/PokerPlanning.Api/PokerPlanning.Api.csproj -o .codex-run/review-build-api`.
- Frontend production build passes: `npm run build`.

## Launch gate

For this app's realistic risk profile, do not block launch on broad internet-abuse items like email rate limiting. Do block on:

1. ~~Fixing participant identity so room actions cannot be authorized by a caller-supplied GUID.~~ **Done** (see #1 resolution).
2. ~~Making password-protected rooms actually require successful join/access for room reads, history, and SignalR group membership.~~ **Done** (see #2 resolution).
3. Fixing concurrent vote overwrites if you expect more than one participant to vote at nearly the same time. *(still open — issue #3)*

Everything else can be treated as post-launch hardening or accepted portfolio-project tradeoff.
