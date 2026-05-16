# Use Case: Reconnect And Current Round Persistence

## Goal

Verify that current round state survives page reloads and reconnects through Redis-backed live state.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Reload the page through the browser and verify both visible UI state and `GET /rooms/{id}` current-round state after reconnect.

## Setup

Use one browser context for reload testing. Use two contexts for live reconnect testing.

Open `http://localhost:4200`.

## Test Steps

1. Create a room as `Owner`.
2. Start a round with title `Reload persistence check`.
3. Select card `3`.
4. Reload the room page.
5. Wait for SignalR to reconnect.
6. Verify the current round is still in `Voting`.
7. Verify the round title is still visible.
8. Verify the own vote is still represented if the UI supports restoring it.
9. Reveal votes.
10. Reload again and verify the revealed state persists until the round is ended.

## Expected Results

- Reload does not clear the active round.
- `GET /rooms/{id}` returns a non-null `currentRound` after reload.
- SignalR reconnects and rejoins the room group.
- The current phase remains accurate after reload.
- Revealed vote state remains accurate after reload.
- Ending the round clears the Redis current round state.

## Failure Report Requirements

If this fails, include:

- `GET /rooms/{id}` response before and after reload.
- Visible room status before and after reload.
- SignalR connection state after reload.
- Any API or console errors.
