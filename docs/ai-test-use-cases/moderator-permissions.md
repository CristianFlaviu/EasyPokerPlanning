# Use Case: Moderator Permissions

## Goal

Verify that only the owner or a promoted moderator can start, reveal, reset, and end rounds, and that ordinary voters cannot use moderator actions.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Use isolated browser contexts for owner and participant, and verify permission changes live without relying on manual reloads.

## Setup

Use at least two isolated browser contexts:

- Context A: owner.
- Context B: ordinary voter, later promoted moderator.

Open `http://localhost:4200`.

## Test Steps

1. Context A creates a room as `Owner`.
2. Context B joins as `Bob`.
3. Verify context B cannot see start/reveal/reset/end controls as an ordinary voter.
4. Context A promotes Bob to moderator.
5. Verify context B receives moderator status live.
6. Context B starts a round.
7. Context B reveals votes.
8. Context B resets the round.
9. Context A demotes Bob.
10. Verify context B loses moderator controls live.

## Expected Results

- Ordinary voters cannot start, reveal, reset, or end rounds from the UI.
- Owner can promote and demote moderators.
- Moderator changes are broadcast through SignalR.
- Promoted moderator can perform moderator actions.
- Demoted participant loses moderator controls.
- Backend rejects moderator actions from non-owner, non-moderator callers if invoked directly.

## Failure Report Requirements

If this fails, include:

- Participant IDs for owner and Bob.
- Whether the controls were visible when they should not be.
- Promote/demote API status codes and responses.
- Failed moderator action API status codes and responses.
- SignalR connection state in both contexts.
