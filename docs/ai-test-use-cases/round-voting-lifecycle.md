# Use Case: Round Voting Lifecycle

## Goal

Verify the core planning poker round lifecycle: start, vote, reveal, end, and return to waiting state.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Verify visible state transitions after start, vote, reveal, and end; inspect network responses if any transition does not happen.

## Setup

Use one browser context. A second context is optional but useful for vote visibility.

Open `http://localhost:4200`.

## Test Steps

1. Create a room as `Owner`.
2. Start a round with title `Estimate login page`.
3. Verify the room changes from waiting state to `Voting`.
4. Select card `5`.
5. Verify the current user sees its own selected card.
6. Reveal votes.
7. Verify the room changes to `Revealed` and the selected card is visible.
8. End the round with final estimate `5`.
9. Verify the room returns to no active round.

## Expected Results

- `POST /rooms/{id}/rounds` succeeds.
- The UI shows `Voting` after starting the round.
- The Fibonacci deck is exactly `1, 2, 3, 5, 8, 13, 21, ?`.
- During voting, the own selected card is visible to the voter.
- During voting, other participants' card values are hidden if a second context is used.
- `POST /rooms/{id}/round/reveal` succeeds.
- Revealed votes show card values.
- `POST /rooms/{id}/round/end` succeeds.
- The active round clears after ending.

## Failure Report Requirements

If this fails, include:

- Which lifecycle step failed.
- All relevant round API response codes and bodies.
- Visible room status text before and after the failed action.
- SignalR connection state.
- Any console errors.
