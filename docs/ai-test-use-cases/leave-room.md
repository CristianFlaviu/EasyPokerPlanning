# Use Case: Leave Room

## Goal

Verify that a non-owner can leave a room, disappear from active participants, and preserve completed voting history where applicable.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Use isolated browser contexts for owner and leaving participant, and verify live participant removal without reloading the owner context.

## Setup

Use two isolated browser contexts:

- Context A: owner.
- Context B: participant who leaves.

Open `http://localhost:4200`.

## Test Steps

1. Context A creates a room.
2. Context B joins as `Alice`.
3. Context A starts a round.
4. Context B votes with card `8`.
5. Context A reveals and ends the round with final estimate `8`.
6. Context B clicks `Leave room`.
7. Verify context B navigates to history.
8. Verify context A sees Alice removed from active participants without reload.
9. Verify context B's history still includes the room and completed round.

## Expected Results

- Non-owner has a visible leave action.
- Owner does not have a leave action.
- `DELETE /rooms/{id}/participants/me` succeeds for non-owner.
- Leaving removes the participant from active room seats.
- Leaving removes any active vote if there is an active round.
- Completed-round history remains discoverable for a voter who later leaves.

## Failure Report Requirements

If this fails, include:

- Participant IDs for owner and Alice.
- Leave API status code and response.
- Context B route after leaving.
- Context A participant list before and after leaving.
- History page result after leaving.
