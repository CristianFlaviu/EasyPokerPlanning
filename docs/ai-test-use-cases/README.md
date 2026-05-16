# AI Test Use Cases

This folder contains focused browser/API test prompts for AI agents. Each file is a self-contained use case that an agent can pick up, run, and report on without deciding what "test everything" means.

## How To Use

Give an AI agent one file at a time and ask it to follow the instructions exactly.

For browser-facing use cases, tell the agent to use the Browser plugin/skill. The general testing guidance lives in:

- `docs/ai-test-use-cases/browser-testing-guide.md`

Default startup commands:

```powershell
dotnet run --project backend/src/PokerPlanning.AppHost
```

```powershell
cd frontend
npm start
```

Default local URLs:

- Frontend: `http://localhost:4200`
- API: `http://localhost:5218`

## Required Agent Report

Every test report should include:

- Use case file tested.
- Environment used, including frontend URL and API URL.
- Whether the test passed or failed.
- Exact reproduction steps for every failure.
- Expected behavior versus actual behavior.
- Browser console errors, network errors, or API responses relevant to failures.
- Files likely involved if a bug is found.
- Whether any code was changed.
- Verification commands run.

Do not ask an agent to run every file at once unless you want a full regression pass.
