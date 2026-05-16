# Use Case: Observer Role

## Goal

Verify that participants can switch between voter and observer, and observers cannot vote.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Verify both the visible deck state and the role text after switching between voter and observer.

## Setup

Use one browser context for a basic test. Use two contexts if checking live role propagation.

Open `http://localhost:4200`.

## Test Steps

1. Create a room as `Owner`.
2. Start a round.
3. Switch the current user from `Voter` to `Observer`.
4. Verify the voting deck is no longer available to the observer.
5. Try to submit a vote directly through the API as the observer, if practical.
6. Switch back to `Voter`.
7. Select a card.

## Expected Results

- Switching to observer sends `POST /rooms/{id}/participants/me/role`.
- Observer status is reflected in the room UI.
- Observer cannot vote from the UI.
- Direct observer vote attempts are rejected by the backend.
- Switching back to voter restores voting ability while the round is still in `Voting`.
- Role changes are reflected live in other connected clients if a second context is used.

## Failure Report Requirements

If this fails, include:

- Role change API status codes and response bodies.
- Whether the deck remained visible while observer.
- Direct vote API response if tested.
- Visible participant role before and after switching.
