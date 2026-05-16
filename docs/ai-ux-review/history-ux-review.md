# UX Review: History

## Goal

Review whether completed rounds and previous rooms are easy to find and understand.

## Read First

- `docs/ai-ux-review/review-guide.md`
- `frontend/CLAUDE.md`
- `docs/domain-model.md`

## Tooling

Use the Browser plugin/skill. Do not change code.

## Review Steps

1. Create a room.
2. Complete at least one round with a final estimate.
3. Open `/history`.
4. Review room list clarity.
5. Review completed-round detail clarity.
6. Review empty state in a fresh browser context.
7. Review mobile/narrow viewport behavior.

## Questions To Answer

- Can a participant tell which rooms they have used?
- Are completed rounds easy to inspect?
- Are vote snapshots and final estimates readable?
- Is the empty state useful?
- Is the relationship between active rooms and historical rooms clear?

## Output Required

List history UX findings and propose improvements. Include likely files involved, especially `frontend/src/app/features/history/*`.

