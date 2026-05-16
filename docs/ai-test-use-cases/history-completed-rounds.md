# Use Case: History Completed Rounds

## Goal

Verify that completed rounds are persisted and visible from the history page.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Verify the visible history page and inspect history API responses if the completed round is missing.

## Setup

Use one browser context.

Open `http://localhost:4200`.

## Test Steps

1. Create a room as `Owner`.
2. Start a round with title `History test round`.
3. Vote with card `13`.
4. Reveal votes.
5. End the round with final estimate `13`.
6. Navigate to `/history`.
7. Verify the room appears in the participant room list.
8. Open or select the room history detail.
9. Verify the completed round appears with title, vote, and final estimate.

## Expected Results

- `GET /rooms/history` returns the room for the current participant.
- `GET /rooms/{id}/history` returns the completed round.
- The history page shows the completed round count.
- The completed round title is visible.
- The final estimate is visible.
- The vote snapshot contains the selected card.

## Failure Report Requirements

If this fails, include:

- History API response bodies.
- Visible history page text.
- Current participant ID.
- Room ID.
- Whether the round was ended before opening history.
