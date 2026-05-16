# Use Case: Join Room From Shared Link

## Goal

Verify that a second anonymous participant can open a shared room link, join by display name, and appear live for existing participants.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Use two isolated browser contexts so owner and joined participant have different `pp.participantId` values.

## Setup

Use two isolated browser contexts so each has a different `pp.participantId`.

Open `http://localhost:4200`.

## Test Steps

1. In browser context A, create a room as `Owner`.
2. Copy the room URL.
3. In browser context B, open the copied room URL.
4. Verify context B sees the join form rather than the active room.
5. Join as `Alice` with role `Voter`.
6. Observe context A without reloading.

## Expected Results

- Context B sends `POST /rooms/{id}/join`.
- Context B enters the room after joining.
- Context B sees itself as a voter.
- Context A receives the joined participant live through SignalR.
- Both contexts show the same participant list.
- The room URL remains shareable and stable.

## Failure Report Requirements

If this fails, include:

- Both browser context participant IDs.
- The room URL used.
- `POST /rooms/{id}/join` status code and response body.
- Whether context A updated without a reload.
- SignalR connection state in both contexts.
