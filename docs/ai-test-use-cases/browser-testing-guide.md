# Browser Testing Guide For AI Agents

Use this guide for every AI test use case in this folder that exercises the web UI.

## Required Tooling

Use the Browser plugin/skill for localhost UI testing. Do not replace browser testing with only API calls unless the use case explicitly says API-only testing is acceptable.

Use browser automation to:

- Open `http://localhost:4200`.
- Click through the UI.
- Use isolated browser contexts when different participants are required.
- Inspect visible page state after every important action.
- Check browser console errors.
- Check relevant network requests and response codes when a step fails.
- Capture screenshots only when they help explain a failure or the user explicitly asks for them.

## When To Use Multiple Browser Contexts

Use isolated contexts whenever a use case needs separate anonymous participants. This matters because identity is stored in local storage as `pp.participantId`.

Examples:

- Owner and joined participant.
- Owner and moderator.
- Owner and observer.
- Two voters verifying hidden votes before reveal.

Each context should have its own `pp.participantId`.

## Startup Expectations

Use the running app if it is already available. Otherwise start:

```powershell
dotnet run --project backend/src/PokerPlanning.AppHost
```

```powershell
cd frontend
npm start
```

Default URLs:

- Frontend: `http://localhost:4200`
- API: `http://localhost:5218`

If a port differs, report the actual URL used.

## Testing Discipline

Before clicking, verify the target is visible and unique enough to act on. After clicking, verify the next visible state instead of assuming the action worked.

For every major action, check one of:

- Visible UI state changed as expected.
- Expected network request succeeded.
- Expected SignalR/live update appeared in another browser context.
- Expected API state is visible through a follow-up page load or request.

## Bug Report Format

When a failure is found, report:

- Use case file.
- Step number.
- Exact reproduction steps.
- Expected behavior.
- Actual behavior.
- Browser console errors.
- Network request URL, method, status code, and response body when relevant.
- Current URL and visible page text.
- Participant IDs and room ID when relevant.
- Likely source files.

Do not change code unless the user or use case explicitly allows it. If code is changed, run the relevant verification commands and report them.

