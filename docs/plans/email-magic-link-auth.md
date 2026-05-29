# Login / Sign Up Modal and Email Magic-Link Auth

## Status
Implemented on 2026-05-29.

## Summary
EasyPokerPlanning keeps anonymous room creation as the default while offering optional account sign-in through Google OAuth or email magic links. The app bar exposes `Sign Up` and `Login`, both opening the same dark auth modal with the appropriate active mode.

## Implemented Backend Behavior
- `POST /auth/email/request` accepts `mode`, `email`, optional `displayName`, and `returnUrl`, then sends a one-time login link when appropriate.
- `GET /auth/email/callback?token=...` validates and consumes the token, links or creates the `User`, signs the existing `pp.auth` cookie, and redirects to the validated frontend return URL.
- Existing users are merged by normalized email, so Google and email login can point to the same account.
- Login requests for unknown email addresses return accepted without creating an account.
- New email-only users get their initial display name from the part of the email before `@`; editable profile names are planned for a later slice.
- Email magic-link tokens are stored hashed, expire after 15 minutes, and are single-use.
- `users.logins` JSON storage was migrated to `user_logins` with a unique `(provider, subject)` index.

## Implemented Frontend Behavior
- Anonymous app-bar state shows `Sign Up` and `Login`.
- Signed-in app-bar state still shows the user avatar/name menu and sign-out action.
- Auth modal supports Google and email magic-link flows.
- Sign-up asks for email only.
- Login asks for email.
- Successful email request shows an in-dialog check-your-inbox state with `Resend email` and `Use another email` actions.
- Auth dialog readability was tuned for the dark surface, including brighter field labels, input text, outlines, and error text.

## SMTP Configuration
Email delivery uses MailKit and Gmail SMTP. Configure these values through user-secrets locally or Fly secrets in production:

- `Email:Smtp:Host` defaults to `smtp.gmail.com`
- `Email:Smtp:Port` defaults to `587`
- `Email:Smtp:FromEmail`
- `Email:Smtp:UserName`
- `Email:Smtp:Password`
- `Email:Smtp:FromName` defaults to `Easy Poker`

## Verification
- `dotnet build backend/src/PokerPlanning.Api/PokerPlanning.Api.csproj -o .codex-run/build-api`
- `npm run build` in `frontend/`
