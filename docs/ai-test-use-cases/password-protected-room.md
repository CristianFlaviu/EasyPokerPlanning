# Use Case: Password-Protected Room

## Goal

Verify that password-protected rooms require the correct password and reject incorrect joins.

## Read First

- `CLAUDE.md`
- `AGENTS.md`
- `frontend/CLAUDE.md`
- `backend/CLAUDE.md`
- `docs/domain-model.md`
- `docs/progress.md`
- `docs/ai-test-use-cases/browser-testing-guide.md`

## Tooling

Use the Browser plugin/skill for this UI test. Use isolated browser contexts and inspect the failed join network response when testing the incorrect password path.

## Setup

Use two isolated browser contexts.

Open `http://localhost:4200`.

## Test Steps

1. In browser context A, create a room with password `secret123`.
2. Copy the room URL.
3. In browser context B, open the room URL.
4. Try to join as `WrongPasswordUser` with password `wrong`.
5. Verify the join is rejected and context B remains on the join form.
6. Join again as `Alice` with password `secret123`.

## Expected Results

- The room metadata indicates it is password protected.
- The join form shows a password field.
- Incorrect password join fails with a user-visible error.
- Correct password join succeeds.
- The plaintext password is never visible after submission except in the input field while typed.
- Context A receives the new participant through SignalR after the successful join.

## Failure Report Requirements

If this fails, include:

- `POST /rooms/{id}/join` response for wrong password.
- `POST /rooms/{id}/join` response for correct password.
- Screenshot or text description of the join form state after each attempt.
- Any console errors.
