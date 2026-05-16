# UX Review: Lobby And Create Room

## Goal

Review the lobby and room creation experience for clarity, confidence, and first-use quality.

## Read First

- `docs/ai-ux-review/review-guide.md`
- `frontend/CLAUDE.md`
- `docs/domain-model.md`

## Tooling

Use the Browser plugin/skill. Do not change code.

## Review Steps

1. Open `http://localhost:4200`.
2. Review the lobby as a first-time visitor.
3. Test empty form submission behavior.
4. Test valid room creation.
5. Test password-protected room creation if the UI exposes it.
6. Review mobile/narrow viewport layout.

## Questions To Answer

- Is it immediately clear what this app does?
- Is the create-room form prominent without feeling like a marketing page?
- Are required fields and validation states obvious?
- Does password protection feel optional and understandable?
- Is the transition into the room smooth?

## Output Required

List findings by severity, then propose specific layout/copy/interaction improvements. Include likely files involved, especially `frontend/src/app/features/lobby/*`.

