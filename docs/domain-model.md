# Domain Model

This is the single source of truth for poker planning business rules. The domain code in `backend/src/PokerPlanning.Domain/` must reflect this document. If a feature request contradicts this doc, update the doc first, then the code.

## Ubiquitous language
- **Room** — a persistent space where a team plays poker planning. Has an owner, optional password, list of participants, history of completed rounds. Identified by a shareable link.
- **Participant** — someone who has joined a room. Identified by a client-generated `participantId` (per-browser, persisted in localStorage) plus a display name they choose at join time.
- **Owner** — the participant who created the room. Has full control. Can promote others to moderator.
- **Moderator** — a participant promoted by the owner. Can start/reveal/reset rounds.
- **Voter** — any participant who isn't an observer. Can submit a card in the current round.
- **Round** — one voting cycle. Has an optional title (what you're voting on), a phase, votes, and a final estimate once locked.
- **Card** — one of the fixed Fibonacci values: `1, 2, 3, 5, 8, 13, 21, ?`. The `?` card means "I don't know / need more info."
- **Vote** — a participant's chosen card for the current round. Hidden from others until reveal.
- **Reveal** — the action that transitions the round from `Voting` to `Revealed`, making all votes visible.
- **Reset** — discards votes and returns to `Voting` for the same round.
- **End Round** — finalizes the round (optionally with a chosen final estimate) and archives it to history. A new round can then be started.

## Aggregates

### `Room` (aggregate root)
```
RoomId : Guid (value object)
Name : string (1–80 chars)
PasswordHash : string? (null = open room)
OwnerId : ParticipantId
Participants : list<Participant>     // currently in the room
ModeratorIds : set<ParticipantId>    // subset of participants
CurrentRound : Round? (null between rounds)
History : list<CompletedRound>       // not loaded by default; queried separately
CreatedAt : DateTimeOffset
ArchivedAt : DateTimeOffset?         // null = active
```

### `Participant` (entity within Room)
```
ParticipantId : Guid (value object)
DisplayName : string (1–40 chars)
Role : enum { Voter, Observer }
JoinedAt : DateTimeOffset
```

### `Round` (entity within Room — exactly zero or one active at a time)
```
RoundId : Guid
Title : string? (optional, max 200 chars)
Phase : enum { Voting, Revealed }
Votes : map<ParticipantId, Card>
StartedAt : DateTimeOffset
```

### `CompletedRound` (entity within Room.History)
```
RoundId : Guid
Title : string?
Votes : map<ParticipantId, Card>     // snapshot
FinalEstimate : Card?                // null if no consensus reached / round skipped
StartedAt : DateTimeOffset
EndedAt : DateTimeOffset
```

## Value objects
- `Card` — one of the eight allowed values. Construct via factory; invalid values rejected.
- `RoomPassword` — wraps the plaintext briefly during create/join; never stored. Hash via Argon2id.

## Invariants
1. A Room has exactly one Owner. Owner cannot leave the room while it's active (must transfer ownership first — out of scope for v1, can be enforced by simply not allowing it).
2. A Room has 0 or 1 `CurrentRound`. You cannot start a new round while one is active (must end the current one first).
3. A participant can vote at most once per round. Re-submitting replaces the previous vote.
4. Votes are accepted only when `CurrentRound.Phase == Voting`.
5. Reveal transitions `Voting → Revealed`. From `Revealed`, only Reset or EndRound are valid.
6. Only the Owner or a Moderator can: start a round, reveal, reset, end the round, promote/demote moderators.
7. The card deck is fixed: `1, 2, 3, 5, 8, 13, 21, ?`. No customization in v1.
8. Observers (role) cannot submit votes but can be present.
9. Display name uniqueness is **not** enforced — two "Alice"s are allowed in the same room. They're distinguished by participantId.
10. Reveal requires at least one submitted vote. Empty rounds cannot be revealed or archived to history.

## State machine — Round lifecycle
```
        ┌──────────────┐
   start│              │
   ────▶│   Voting     │◀────────┐
        │              │  reset  │
        └──────┬───────┘         │
               │ reveal           │
               ▼                  │
        ┌──────────────┐          │
        │  Revealed    │──────────┘
        │              │
        └──────┬───────┘
               │ endRound
               ▼
       (archived to History,
        CurrentRound = null)
```

Only **start** transitions from "no round" to `Voting`. Only **endRound** transitions out of `Revealed` to "no round."

## Commands & corresponding domain methods

| Command            | Caller       | Domain method on `Room`           | Notifies via SignalR |
|--------------------|--------------|------------------------------------|----------------------|
| CreateRoom         | anyone       | `Room.Create(name, password?, owner)` | n/a (HTTP only)    |
| JoinRoom           | anyone       | `room.AddParticipant(p, password?)`| `ParticipantJoined`  |
| LeaveRoom          | participant  | `room.RemoveParticipant(id)`       | `ParticipantLeft`    |
| PromoteModerator   | owner        | `room.PromoteToModerator(id)`      | `ModeratorPromoted`  |
| DemoteModerator    | owner        | `room.DemoteFromModerator(id)`     | `ModeratorDemoted`   |
| ChangeRole         | self         | `participant.SetRole(role)`        | `ParticipantRoleChanged` |
| StartRound         | owner/mod    | `room.StartRound(title?)`          | `RoundStarted`       |
| SubmitVote         | voter        | `room.SubmitVote(participantId, card)` | `VoteSubmitted` (no card value) |
| RevealVotes        | owner/mod    | `room.RevealVotes()`               | `VotesRevealed` (full data) |
| ResetRound         | owner/mod    | `room.ResetCurrentRound()`         | `RoundReset`         |
| EndRound           | owner/mod    | `room.EndRound(finalEstimate?)`    | `RoundEnded`         |

## Vote visibility rules
- **During `Voting`:** clients see which participants have voted (e.g. a checkmark next to the name) but not what was voted. Voters see their own card.
- **During `Revealed`:** all cards are visible to everyone.

The `VoteSubmitted` SignalR event carries only `participantId` + a "has voted" flag. The `VotesRevealed` event carries the full vote map.

## Persistence split
- **PostgreSQL** stores: Room (metadata, password hash, owner, moderators, archived flag), CompletedRound (with vote snapshot as JSONB), Participant identity (id + display name + when they were last in the room).
- **Redis** stores: live Room state — current round, current votes, currently-connected participants and their connection IDs.
- On `EndRound`, persist the CompletedRound to Postgres and clear the round from Redis.
- On the last participant leaving, the room remains in Postgres but live state is cleared from Redis.
- On rejoining a room with no active round, only the room metadata + history are needed (Postgres only).

## History view
- `GET /rooms/history?participantId=...` returns rooms the participant has been in, with summary stats per room (number of rounds completed, when last active).
- `GET /rooms/{id}/history` returns the list of `CompletedRound`s for a specific room.
- A participant only sees rooms they've been in (matched by their participantId — note: this is best-effort identity since participantId is browser-local).

## Out of scope for v1
- Multiple card decks (Fibonacci only)
- Story tracking / ticket integration (only round titles)
- Ownership transfer
- Account-based identity (anonymous only)
- Real password recovery (passwords are room-specific; lose it, you re-create the room)
- Spectator/observer counts in history
- Time-boxed voting (no auto-reveal)
