# UX Review: Room Experience

## Goal

Review the active planning room experience across waiting, voting, revealed, moderator, and participant states.

## Read First

- `docs/ai-ux-review/review-guide.md`
- `frontend/CLAUDE.md`
- `docs/domain-model.md`

## Tooling

Use the Browser plugin/skill. Use isolated browser contexts when reviewing multi-participant behavior. Do not change code.

## Review Steps

1. Create a room as owner.
2. Join with a second participant in another browser context.
3. Review owner waiting state before a round starts.
4. Review participant waiting state.
5. Start a round.
6. Review voting state before and after each participant votes.
7. Reveal votes.
8. Review stats, consensus, outlier, and final-estimate controls.
9. Reset a round and review whether the transition is understandable.
10. End a round and review whether users know what happened.
11. Check mobile/narrow viewport behavior.

## Questions To Answer

- Is the next action always clear?
- Are moderator-only controls understandable?
- Are role labels, owner/moderator labels, and observer state clear?
- Does the table layout help or distract?
- Are vote visibility rules clear before reveal?
- Is the reveal/results state useful enough for a planning session?

## Output Required

Produce a prioritized room UX improvement plan. Separate quick polish from deeper layout/flow redesigns. Include likely files involved, especially `frontend/src/app/features/room/*` and shared components.

