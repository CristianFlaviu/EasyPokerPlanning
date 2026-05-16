# BUG: Round can be revealed and ended with zero votes, polluting history

## Use Case
Observed during `docs/ai-test-use-cases/moderator-permissions.md` and `docs/ai-test-use-cases/history-completed-rounds.md`.

## Environment
- Frontend: `http://localhost:4200`
- API: `http://localhost:5218`
- Date: 2026-05-16
- Branch: main

## Summary
The `Reveal cards` and `End without estimate` actions remain enabled while a round has zero submitted votes. Moderators can therefore complete a round in which nobody picked a card, and that empty round is persisted to history as `Untitled round / 0 votes`. The history list fills with meaningless entries.

Reproduced in room `Mod Test` (id `7c2742e6-1b2c-4a14-92ff-2d33f013145a`) — `/history` shows `Untitled round  0 votes` alongside real rounds.

## Reproduction
1. Create a room as Owner.
2. Start a round (with or without title) — counter shows `0 / 2 voted`.
3. Without anyone voting, click `Reveal cards`.
   - State transitions to `REVEALED`, distribution panel renders empty.
4. Click `End without estimate`.
5. Open `/history` → the room shows an extra completed round with `0 votes` and (often) no title.

## Expected
Either:
- `Reveal cards` disabled until at least one vote is submitted, OR
- `End without estimate` / `End round` not persisted to history when `Votes.Count == 0` (the round just clears back to `WAITING FOR ROUND`).

Preferred: disable `Reveal cards` at `0 / N voted`. An empty round has nothing to reveal.

## Actual
- `Reveal cards` is enabled at `0 / 2 voted`.
- After reveal, `End without estimate` writes a `CompletedRound` with empty `Votes` and `FinalEstimate = null`.
- `/history` lists it as `Untitled round  0 votes`.

## Why this matters
- `docs/domain-model.md` does not forbid this transition (gap in invariants), but the resulting state contradicts the spirit of "Round = one voting cycle." A cycle with zero votes is not a cycle.
- History becomes noisy and hard to scan. Real rounds get mixed with empty ones from misclicks or test/reset flows.

## Likely source files
- `backend/src/PokerPlanning.Domain/` — `Room.RevealVotes()` should fail (`Result.Failure`) when `CurrentRound.Votes.Count == 0`.
- `backend/src/PokerPlanning.Domain/` — `Room.EndRound(...)` should either refuse to archive an empty round or skip the history append when there are no votes (depending on chosen rule).
- `frontend/src/app/features/room/` — once the domain method returns failure, the `Reveal cards` button should be `[disabled]` while `votedCount() === 0` so the user does not even attempt it.

## Suggested invariant to add to `docs/domain-model.md`
> 10. `Reveal` requires `CurrentRound.Votes.Count >= 1`. Reveal on an empty round is rejected.

## Repro data
- Room id: `7c2742e6-1b2c-4a14-92ff-2d33f013145a`
- History entry seen: `Untitled round` / `0 votes`
- Console errors: none.
- Network: `POST /rooms/{id}/round/reveal` returned `204` and `POST /rooms/{id}/round/end` returned `204` despite zero votes.

## Code changed?
No.
