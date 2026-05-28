# BUG: Production Google sign-in fails because auth cookie is third-party

## Use Case
Google sign-in end-to-end against the deployed app (Cloudflare Pages frontend + Fly.io API). See `docs/plans/google-signin.md` for the design.

## Environment
- Frontend: `https://easypokerplanning.pages.dev` (Cloudflare Pages)
- API: `https://poker-planning-api-frosty-current-4436.fly.dev` (Fly.io)
- Backend release: cookie posture is `SameSite=None; Secure; HttpOnly`, path `/`, domain `<api>.fly.dev`
- Browser: Chrome (default settings, third-party cookies blocked)
- Date: 2026-05-28
- Branch: main

## Summary
After completing Google OAuth on the production deployment, the cookie is set on the API domain but the browser refuses to attach it on subsequent calls to `/auth/me` because the request is cross-site (`pages.dev` → `fly.dev` are different eTLD+1 → third-party). `/auth/me` returns `204 No Content` instead of the signed-in `UserDto`, so the app bar stays in the signed-out state and the lobby never prefills.

Local dev (`http://localhost:4200` → `http://localhost:5218`) is unaffected because it's same-site, even with the same cookie posture.

## Chosen remediation
Use the purchased custom domain:
- Frontend: `https://poker-planning-online.site`
- API: `https://api.poker-planning-online.site`

Both hosts share the same registrable domain (`poker-planning-online.site`), so the API cookie is same-site instead of third-party when the frontend calls `/auth/me`.

## Reproduction
1. Open `https://easypokerplanning.pages.dev` in a Chrome window with default privacy settings (third-party cookies blocked).
2. Click `Sign in with Google` in the app bar.
3. Pick a Google account on the consent screen.
4. Browser redirects back to `https://easypokerplanning.pages.dev/`.
5. App bar still shows `Sign in with Google` — not the signed-in name/avatar.

## Expected
- App bar shows the signed-in user's avatar + display name.
- Lobby `Your display name` field pre-fills from the Google profile.
- `GET /auth/me` returns `200 OK` with `{ id, email, displayName, avatarUrl }`.

## Actual
- App bar still shows `Sign in with Google`.
- `GET /auth/me` returns `204 No Content`.
- DevTools URL bar shows the "eye-slash" third-party-cookies-blocked indicator.

## Evidence
DevTools → Application → Cookies (under `https://poker-planning-api-frosty-current-4436.fly.dev`):
- `pp.auth` is present, `Secure ✓`, `HttpOnly ✓`, `SameSite=None`, `Path=/`, `Domain=<api>.fly.dev`.
- The cookie was correctly written by the API in response to the OAuth callback.

DevTools → Network → `GET https://poker-planning-api-frosty-current-4436.fly.dev/auth/me`:
- Status: `204 No Content`.
- Response headers: `Access-Control-Allow-Credentials: true`, `Access-Control-Allow-Origin: https://easypokerplanning.pages.dev` (CORS is fine).
- **Request headers: no `Cookie` line.** The browser silently dropped `pp.auth` because the request is cross-site and Chrome currently blocks third-party cookies by default.

No console errors related to the sign-in flow (CORS is correctly configured for credentials).

## Root cause
Cross-site cookie. The auth cookie is issued by `<api>.fly.dev` and the frontend lives on `<frontend>.pages.dev` — different eTLD+1, so any request the frontend makes to the API counts as a third-party context from the browser's perspective. Chrome (and Safari, Brave, strict-mode Firefox) blocks third-party cookies, so the cookie never travels back, and the API treats every request as anonymous.

`SameSite=None; Secure` is the **correct** cookie posture for cross-site auth — the issue is purely browser-side third-party cookie blocking, not server config or CORS.

## Validating the diagnosis (per-user, not a fix)
In Chrome, click the eye-slash icon in the URL bar → "Site not working?" → temporarily allow third-party cookies on this site → reload and sign in again. `/auth/me` returns 200, app bar shows the avatar. This proves the issue is third-party blocking, nothing else.

## Proposed fixes (pick one)

### Option A — Cloudflare Pages function proxy (recommended, no domain purchase)
Add a Pages Function under `frontend/functions/api/[[path]].ts` that proxies `https://easypokerplanning.pages.dev/api/*` to `https://<api>.fly.dev/*`. Flip `environment.prod.ts` `apiBaseUrl` to `'/api'`. Browser sees only `pages.dev` → cookie is first-party → no third-party block.

Required changes:
- New file: `frontend/functions/api/[[path]].ts`.
- `environment.prod.ts`: `apiBaseUrl: '/api'`.
- Google Cloud Console redirect URI updated to `https://easypokerplanning.pages.dev/api/auth/google/callback`. Backend `CallbackPath` stays `/auth/google/callback` because the proxy strips the `/api` prefix.
- SignalR negotiate + WebSocket upgrade needs proxy passthrough. Pages Functions support WebSocket via `fetch` with `Upgrade` headers; needs verification.
- Pages Functions free tier: 100k req/day — fine for portfolio.

### Option B — Custom domain (best long-term)
Buy a domain (e.g. `easypokerplanning.com`). Point apex/`www` → Pages, `api.easypokerplanning.com` → Fly. Same eTLD+1 → first-party cookie everywhere. Permanent fix, but costs ~$10/yr and a small DNS setup.

### Option C — Accept the limitation
Document "Google sign-in requires same-site context. Works in Chrome with third-party cookies enabled. Does not work in Safari, Brave, or Firefox strict mode by default." Acceptable for a portfolio piece if cross-browser sign-in is not a goal. Anonymous flow keeps working everywhere.

## Likely source files (if going with Option A)
- `frontend/functions/api/[[path]].ts` — new Pages Function (proxy).
- `frontend/src/environments/environment.prod.ts` — flip `apiBaseUrl` to `'/api'`.
- `backend/src/PokerPlanning.Api/Program.cs` — no change needed (cookie posture already correct).
- Google Cloud Console (out-of-tree config) — add `https://easypokerplanning.pages.dev/api/auth/google/callback` to Authorised redirect URIs.

## Verification commands run
None — no code was changed for this report. Diagnosis via DevTools only.

## Code changed?
No.

## Related
- `docs/plans/google-signin.md` — original sign-in plan (Phase 1 + 2 shipped).
- `DEPLOYMENT.md` — Fly secret + Google Cloud Console setup.
