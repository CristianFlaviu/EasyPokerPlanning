# BUG: Own vote not restored on reload during Voting phase

## Use Case
`docs/ai-test-use-cases/reconnect-current-round.md`, step 8.

## Environment
- Frontend: `http://localhost:4200`
- API: `http://localhost:5218`
- Date: 2026-05-16
- Branch: main

## Summary
After a voter submits a card and reloads the room during the `Voting` phase, the UI does not restore their own selected card. The vote is still counted by the server (counter shows `1 / 2 voted`), but the "Your pick" panel shows `Tap a card to lock in your vote.` instead of `Selected: N.`.

Per `docs/domain-model.md` (Vote visibility rules): "During Voting: ... Voters see their own card." The reload breaks this — voters cannot see their own card without re-clicking it.

## Reproduction
1. Create room as Owner (or join as Voter).
2. Owner promotes self (already moderator) and starts round titled `Reload persistence check`.
3. Click card `3`. UI shows `Selected: 3. Change anytime before reveal.` and counter `1 / 2 voted`.
4. Hit browser reload (`http://localhost:4200/room/{roomId}`).
5. After reconnect, UI shows phase `VOTING`, title `Reload persistence check`, counter `1 / 2 voted`, but the "Your pick" panel shows `Tap a card to lock in your vote.` with no card highlighted.

## Expected
- Own selected card highlighted (`Card 3` `aria-pressed="true"`).
- Status text reads `Selected: 3. Change anytime before reveal.`

## Actual
- No card highlighted in own deck after reload.
- Status text reads `Tap a card to lock in your vote.`
- Server-side state confirms vote exists (counter `1 / 2 voted`).

## Root Cause (likely)
`GET /rooms/{id}` response strips card values from all vote entries pre-reveal, even for the caller's own vote.

Captured response (Owner participantId `d797bede-1178-44a2-a47e-2181b15abe12`, fetched with `X-Participant-Id` header matching):
```json
{
  "currentRound": {
    "id": "44e54dfd-4899-4f9f-bb9a-7ab617ce2065",
    "title": "Reload persistence check",
    "phase": "Voting",
    "votes": [
      {
        "participantId": "d797bede-1178-44a2-a47e-2181b15abe12",
        "card": null,
        "isRevealed": false
      }
    ]
  }
}
```
The `card` field should be the actual card for the requesting participant's own vote (`participantId == caller`), but is `null`.

## Likely source files
- `backend/src/PokerPlanning.Application/Features/GetRoom/` (or wherever room read/DTO mapping lives) — caller-aware vote projection missing.
- `backend/src/PokerPlanning.Api/Endpoints/RoomEndpoints.cs` — GET handler may need to pass caller id into the projection.
- Frontend `core/signalr/` or room service hydration logic — once API returns the value, the room state should hydrate the local selection signal on load.

## Repro environment data
- Room id: `7c2742e6-1b2c-4a14-92ff-2d33f013145a`
- Owner participantId: `d797bede-1178-44a2-a47e-2181b15abe12`
- Bob participantId: `8c8b0371-26f5-4f0b-b64a-bc4339de9c47`
- Browser console errors at reload: none related to this bug.
- SignalR negotiate after reload: `200 OK`.

## Verification commands run
None — no code was changed.

## Code changed?
No.
