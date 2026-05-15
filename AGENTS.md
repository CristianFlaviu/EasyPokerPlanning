# Poker Planning App — Agent Instructions (Codex / OpenAI)

> This file mirrors `CLAUDE.md` and is the entry point for OpenAI Codex / Codex CLI. Nested `AGENTS.md` files exist (or will exist) in `backend/` and `frontend/`. The same content is duplicated across both tools intentionally — keeping them in sync is a deliberate practice.

## How this differs from CLAUDE.md
Functionally identical. Two practical Codex-specific notes:
1. Codex tends to begin work immediately. If a task is non-trivial, **start your prompt with: "Plan first, then implement."** Without that, you may get half-finished work where Claude Code would have asked clarifying questions.
2. Codex's training data on Angular 21 is currently weaker than Claude's. **For frontend tasks, prefer Claude Code unless you explicitly want to test Codex's recovery.** When using Codex on Angular, be more aggressive about reminding it of the rules below in-prompt.

---

## What this project is
A real-time planning poker app for agile estimation. Users create or join a room via a shareable link, optionally protected by a password. Inside the room, participants vote on a Fibonacci card for whatever the moderator is currently estimating; votes are hidden until revealed.

**This is a learning / portfolio project**, not production software. Optimize for clarity, modern patterns, and demonstrating clean architecture.

## Stack (verify before assuming)
- **Backend:** .NET 10 (LTS, released Nov 2025), C# 14, ASP.NET Core Minimal APIs, EF Core 10
- **Frontend:** Angular 21 (released Nov 2025), zoneless, signals-first, standalone components only, Angular Material 3
- **Real-time:** SignalR
- **Data:** PostgreSQL + Redis
- **Orchestration:** .NET Aspire
- **Mediator:** MediatR

If you generate code that looks like older Angular (`*ngIf`, `NgModule`, `@Input()`) or older .NET (controllers everywhere, full MVC), stop and re-check current docs.

## Repository layout
```
poker-planning/
├── CLAUDE.md / AGENTS.md
├── docs/domain-model.md     # read before any feature work
├── backend/
│   ├── CLAUDE.md / AGENTS.md
│   └── src/
│       ├── PokerPlanning.Domain/
│       ├── PokerPlanning.Application/
│       ├── PokerPlanning.Infrastructure/
│       ├── PokerPlanning.Api/
│       └── PokerPlanning.AppHost/
└── frontend/
    ├── CLAUDE.md / AGENTS.md
    └── src/app/{core, shared, features, domain}
```

## Architecture rules — non-negotiable
1. Api → Application → Domain. Infrastructure → Application → Domain. Domain references nothing.
2. Domain logic lives on aggregate methods, never in handlers or services.
3. SignalR hub methods contain no business logic — they dispatch commands and rely on event handlers to broadcast.
4. No EF Core types past Infrastructure.
5. One feature = one folder under `Application/Features/{FeatureName}/`. Mirror `CreateRoom` exactly.
6. Frontend feature boundaries mirror backend feature boundaries.

## Testing policy
This is a learning project — **do not generate unit tests unless explicitly asked.** Integration tests exist only for SignalR flows. Do not add test projects without asking.

## Commands
- `dotnet run --project backend/src/PokerPlanning.AppHost` — start everything
- `npm start` in `frontend/` — start Angular dev server

## What to ask before doing
- Adding packages
- Changing architecture rules
- Touching auth (room passwords)
- Schema migrations on existing tables

## What NOT to do
- `NgModule`, `*ngIf`, `*ngFor`
- `@Input()`/`@Output()` decorators (use `input()`/`output()`)
- Generate Angular from memory without verifying v21 patterns
- Add unit tests proactively
- Cross dependency-direction lines
- Put logic in SignalR hubs
- Call `IHubContext` outside event handlers

## Domain rules — quick reference
See `docs/domain-model.md` for the full spec.
- Anonymous identity via per-browser `participantId` + chosen display name
- Optional room password
- Fixed Fibonacci deck: `1, 2, 3, 5, 8, 13, 21, ?`
- Round lifecycle: Voting → Revealed → (Reset to Voting | EndRound to history)
- Only Owner or Moderator can start/reveal/reset/end rounds
- Pre-reveal: voters see *who* voted, not *what*
- History persisted: list of completed rounds per room
