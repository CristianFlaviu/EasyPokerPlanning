# Use Case: Full V1 Smoke

## Goal

Run a compact end-to-end smoke test that covers the main v1 path without trying to exhaust every edge case.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Use isolated browser contexts for owner and participant, verify live SignalR updates, and collect console/network details for the first failed step.

## Setup

Use the running app if available. Otherwise start:

```powershell
dotnet run --project backend/src/PokerPlanning.AppHost
```

```powershell
cd frontend
npm start
```

Open `http://localhost:4200`.

## Test Steps

1. Create a room as `Owner`.
2. Open the room link in a second isolated browser context.
3. Join as `Alice`.
4. Owner starts a round named `Smoke round`.
5. Alice votes `5`.
6. Owner votes `8`.
7. Verify pre-reveal visibility hides other users' card values.
8. Owner reveals votes.
9. Verify both votes are visible.
10. Owner ends the round as `8`.
11. Alice leaves the room.
12. Owner verifies Alice is removed from active participants.
13. Open history and verify the completed round is present.

## Expected Results

- Create, join, start, vote, reveal, end, leave, and history all succeed.
- SignalR updates participant and round state without manual reloads.
- Vote visibility follows the domain model.
- History persists the completed round.
- No browser console errors occur during the flow.

## Failure Report Requirements

If this fails, include:

- The first failed step.
- Browser console errors.
- Failed network request status and response.
- Current visible UI text.
- Room ID and participant IDs.
- Whether a manual reload changes the outcome.
