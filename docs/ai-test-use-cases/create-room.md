# Use Case: Create Room

## Goal

Verify that a new anonymous owner can create a room from the lobby and land in the room as the owner.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Verify visible page state after each major action and inspect console/network details if a step fails.

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

1. Clear the browser's `pp.participantId` from local storage or use a fresh browser context.
2. Open the lobby.
3. Enter a room name.
4. Enter an owner display name.
5. Leave the password empty.
6. Create the room.
7. Wait for navigation to `/room/{roomId}`.

## Expected Results

- A `POST /rooms` request succeeds.
- The app navigates to a room URL.
- The current user appears in the room participant list.
- The current user is marked as owner.
- The SignalR connection state becomes `connected`.
- The room starts with no active round and shows the waiting/start-round state.

## Failure Report Requirements

If this fails, include:

- The submitted room name and display name.
- The `POST /rooms` status code and response body.
- The current route after submission.
- Relevant console errors.
- Whether `pp.participantId` exists in local storage.
