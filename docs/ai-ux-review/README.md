# AI UX Review Prompts

This folder contains prompts for an AI agent to inspect the product experience, identify UX/design issues, and propose better flows. These are review prompts, not implementation tasks.

## How To Use

Give an agent one file at a time and ask it to follow the prompt exactly.

Example:

```text
Use the Browser plugin to run docs/ai-ux-review/full-product-ux-review.md.
Do not change code. Produce a prioritized UX/design improvement proposal with evidence from the current UI.
```

## Required Tooling

Use the Browser plugin/skill for all page reviews. The agent should inspect the running app visually, not only read source files.

The agent may read source files for context, but recommendations should be grounded in observed user experience.

## Review Output Format

Each review should include:

- Pages and flows reviewed.
- Viewports checked.
- Current UX summary.
- Top friction points, prioritized by user impact.
- Proposed design or flow changes.
- Tradeoffs and implementation risk.
- Suggested implementation slices.
- Screenshots or precise visible-state notes when useful.

## Guardrails

- Do not change code unless explicitly asked.
- Do not propose features that contradict `docs/domain-model.md`.
- Respect Angular 21 and frontend rules from `frontend/CLAUDE.md`.
- Prefer pragmatic v1 improvements over broad product rewrites.
- Keep proposals scoped enough that a future implementation agent can pick them up.

