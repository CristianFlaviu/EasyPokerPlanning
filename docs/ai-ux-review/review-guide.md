# UX Review Guide For AI Agents

Use this guide before running any UX/design review prompt in this folder.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`

## Required Browser Workflow

Use the Browser plugin/skill to inspect the running app.

Use the running app if available. Otherwise start:

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

## What To Inspect

Review at least:

- Lobby/create room page.
- Room page before a round starts.
- Room page while voting.
- Room page after reveal.
- Join-from-shared-link experience.
- History page with at least one completed round.
- Mobile/narrow viewport behavior.
- Desktop/wide viewport behavior.

Use realistic sample names and round titles. If multiple participants are needed, use isolated browser contexts so each has a different `pp.participantId`.

## Evaluation Criteria

Assess:

- Task clarity: can a user understand what to do next?
- Flow efficiency: are key actions easy to find?
- State visibility: connection, role, round phase, votes, and history.
- Moderator versus participant mental model.
- Error recovery: failed join, wrong password, disconnected state.
- Responsive layout and text fit.
- Visual hierarchy and density.
- Accessibility basics: keyboard reachability, focus visibility, contrast, button labels.
- Portfolio quality: whether the experience demonstrates clean product thinking.

## Proposal Rules

Separate findings from proposals.

For each proposal, include:

- Problem it solves.
- User benefit.
- A concrete design direction.
- A low-risk implementation slice.
- Files likely involved.
- Any backend/API impact.

Do not ask for new packages unless clearly justified. Do not propose account-based identity, custom decks, story integrations, ownership transfer, or time-boxed voting unless explicitly framed as out-of-scope future ideas.

