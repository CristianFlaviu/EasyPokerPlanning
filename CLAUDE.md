# Poker Planning App — Agent Instructions (root)

> Nested `CLAUDE.md` files exist in `backend/` and `frontend/` with stack-specific rules. Read those when working inside their respective trees. This file covers project-wide concerns.


## What this project is
A real-time planning poker app for agile estimation. Users create or join a room via a shareable link, optionally protected by a password. Inside the room, participants vote on a Fibonacci card for whatever the moderator is currently estimating; votes are hidden until revealed.

**This is a learning / portfolio project.** Optimize for clarity, modern patterns, and demonstrating clean architecture — not for scale, multi-region, or enterprise auth. It *is* deployed (Fly.io for the API, GitHub Pages for the frontend, via the workflows in `.github/workflows/`); see `DEPLOYMENT.md`. Treat the Docker/nginx/fly config as real, but don't over-engineer for scale.

## Stack (verify before assuming — your training data may be outdated)
- **Backend:** .NET 10 (LTS), C# 14, ASP.NET Core Minimal APIs, EF Core 10
- **Frontend:** Angular 21, zoneless, signals-first, standalone components only (no NgModules), Angular Material 3
- **Real-time:** SignalR
- **Data:** PostgreSQL (durable) + Redis (live room state)
- **Orchestration:** .NET Aspire (AppHost runs everything locally)
- **Mediator:** MediatR (in-process CQRS dispatch)

Angular 21 was released November 2025. .NET 10 was released November 2025 and is LTS. If a code pattern looks familiar from older versions (e.g. `*ngIf`, `NgModule`, MVC controllers everywhere), verify it's still current before using it.

## Repository layout
```
poker-planning/
├── CLAUDE.md                # this file
├── AGENTS.md                # thin pointer to this file + Codex-specific notes
├── docs/
│   ├── domain-model.md      # canonical business rules — read before adding features
│   └── progress.md          # what's done, what's blocked, what's next — read on entry, update at end of each slice
├── backend/
│   ├── CLAUDE.md            # backend-specific rules
│   └── src/
│       ├── PokerPlanning.Domain/
│       ├── PokerPlanning.Application/
│       ├── PokerPlanning.Infrastructure/
│       ├── PokerPlanning.Api/
│       ├── PokerPlanning.AppHost/
│       └── PokerPlanning.ServiceDefaults/  # shared Aspire telemetry/health/resilience
└── frontend/
    ├── CLAUDE.md            # frontend-specific rules
    └── src/app/
        ├── core/            # cross-cutting: signalr service, http interceptors, guards
        ├── shared/          # reusable UI components, directives, pipes
        ├── features/        # one folder per feature (room, lobby, history)
        └── domain/          # TS types mirroring backend DTOs
```

## Architecture rules — non-negotiable
1. **Dependency direction:** Api → Application → Domain. Infrastructure → Application → Domain. Domain references nothing.
2. **Domain logic lives on aggregate methods**, never in handlers or services. A handler loads the aggregate, calls a method, persists. That's it.
3. **SignalR hub methods contain no business logic.** They authenticate, resolve identity, and dispatch a command via MediatR. The result is broadcast via `IHubContext` from inside the handler (or a domain-event handler).
4. **No EF Core types leak past Infrastructure.** Application sees `IRepository` interfaces, never `DbContext` or `DbSet<T>`.
5. **One feature = one folder** under `Application/Features/{FeatureName}/` with `{FeatureName}Command.cs` (or Query), `{FeatureName}Handler.cs`, `{FeatureName}Validator.cs`. Mirror the `CreateRoom` reference slice exactly.
6. **Frontend mirrors backend feature boundaries.** A backend `SubmitVote` feature has a corresponding frontend service method and reactive state flow.

## Testing
This is a learning project — **do not generate unit tests unless explicitly asked.** There are currently **no test projects** in the repo. Do not add one (or any test framework) without asking first.

## Commands you can run
- `dotnet run --project backend/src/PokerPlanning.AppHost` — start everything (API + Postgres + Redis + dashboard)
- `npm start` in `frontend/` — start the Angular dev server
- Aspire dashboard URL is printed on startup; use it to inspect logs and dependencies

## What to ask before doing
- Adding any new NuGet or npm package (especially anything that overlaps with what's already there)
- Changing the architecture rules in this file
- Anything touching auth (room passwords are sensitive — get the design confirmed)
- Schema migrations that alter existing tables

## What NOT to do
- Do not use `NgModule` under any circumstance
- Do not put business logic in SignalR hubs
- Do not use `*ngIf` / `*ngFor` — use `@if` / `@for`
- Do not use `@Input()` / `@Output()` decorators — use `input()` / `output()` signal functions
- Do not generate Angular code from memory — Angular 21 patterns differ significantly from <=v16
- Do not add unit tests proactively (see Testing above)
- Do not write to `Domain/` from `Infrastructure/` or `Api/`
- Do not call `IHubContext` from anywhere except event handlers or specifically marked notification services

## Reference slice
The canonical example to mirror for any new backend feature is `Application/Features/CreateRoom/`. Read it first, then replicate file names, namespaces, validator patterns, and handler structure. If `CreateRoom` doesn't exist yet, ask before scaffolding a new feature.

## Domain rules — quick reference
See `docs/domain-model.md` for the full spec. Summary:
- In-room identity is per-browser `participantId` (localStorage) + chosen display name — usable with no account
- A separate optional **user account** layer exists (Google OAuth + email magic-link, cookie session `pp.auth`). Signed-in creators get `OwnerUserId` on the room. See backend `CLAUDE.md` "Auth model" for the two-layer split
- Room access is open by link, optionally gated by password (BCrypt hash)
- Card deck is fixed: `1, 2, 3, 5, 8, 13, 21, ?` (no other decks)
- A **round** is one voting cycle: start (with optional title) → vote → reveal → reset/next
- Each completed round is persisted as history (title, votes, final estimate if set)
- Only the **owner** or a **moderator** can reveal, reset, or start a new round
- Pre-reveal, voters see *who* has voted but not *what* — only their own card
