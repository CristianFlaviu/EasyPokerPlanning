# Poker Planning App — Agent Instructions (Codex / OpenAI)

> **Single source of truth is `CLAUDE.md`.** This file used to duplicate it; the copy kept drifting out of sync, so it's now a thin pointer. Read `CLAUDE.md` (root) first, then `docs/domain-model.md`, then `docs/progress.md`, then `backend/CLAUDE.md` or `frontend/CLAUDE.md` for the area you're working in.

There are **no nested `AGENTS.md`** files — the nested rules live in `backend/CLAUDE.md` and `frontend/CLAUDE.md`. Read those directly.

## Codex-specific notes
Everything in `CLAUDE.md` applies verbatim. Two practical differences when running under Codex:

1. **Plan before coding.** Codex tends to start editing immediately. For any non-trivial task, begin with "Plan first, then implement" so you don't get half-finished work where clarifying questions were needed.
2. **Frontend is the weak spot.** Codex's Angular 21 knowledge lags Claude's. Prefer Claude Code for frontend work. When using Codex on Angular, re-state the hard rules in-prompt: standalone components only, no `NgModule`, `@if`/`@for` (not `*ngIf`/`*ngFor`), `input()`/`output()` (not `@Input()`/`@Output()`), and verify v21 patterns instead of generating from memory.

## Hard "do not" list (mirrors `CLAUDE.md` — kept here as a Codex guardrail)
- No `NgModule`, `*ngIf`, `*ngFor`, `@Input()`/`@Output()` decorators
- Don't cross the dependency direction: Api → Application → Domain; Infrastructure → Application → Domain; Domain references nothing
- No business logic in SignalR hubs; no `IHubContext` outside event handlers / notification services
- No EF Core types past Infrastructure
- Don't add test projects or new packages without asking
- Touching auth (room passwords, user sign-in)? Confirm the design first

Anything beyond this — domain rules, stack versions, architecture, commands — read `CLAUDE.md`. Do not re-duplicate its content back into this file.
