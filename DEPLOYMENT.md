# Deployment

Current production deployment of EasyPokerPlanning. All services on free tiers.

## Topology

```
┌────────────────────────────────┐
│ Cloudflare Pages               │
│ poker-planning-online.site     │  ← Angular 21 SPA (static)
└───────────────┬────────────────┘
                │ HTTPS + WSS
┌───────────────▼────────────────────────────────┐
│ Fly.io — fra region                            │
│ api.poker-planning-online.site                │  ← .NET 10 API + SignalR
└──────┬──────────────────────────┬──────────────┘
       │                          │
┌──────▼────────────────┐  ┌──────▼─────────────────┐
│ Neon Postgres         │  │ Upstash Redis          │
│ eu-central-1 (Frankfurt)│  │ eu-central-1 (Frankfurt)│
│ ep-plain-hat-...      │  │ better-thrush-126989   │
└───────────────────────┘  └────────────────────────┘
```

## Components

### Frontend — Cloudflare Pages
- **URL:** https://poker-planning-online.site
- **Cloudflare Pages fallback URL:** https://easypokerplanning.pages.dev
- **Preview URLs:** Cloudflare also serves commit previews such as `https://<hash>.easypokerplanning.pages.dev`
- **Project name:** `easypokerplanning`
- **Source:** GitHub repo `CristianFlaviu/EasyPokerPlanning`, branch `main`
- **Build:** runs in GitHub Actions, not Cloudflare's built-in builder (auto-build disabled in Pages settings)
- **Workflow:** `.github/workflows/pages-deploy.yml`
- **Build command:** `npm ci && npm run build` (under `frontend/`)
- **Output directory:** `frontend/dist/poker-planning-frontend/browser`
- **Deploy command:** `wrangler pages deploy ... --project-name=easypokerplanning --branch=main`
- **Trigger:** push to `main` touching `frontend/**` or the workflow file (also manual via "Run workflow")
- **API URL baked at build time** via `frontend/src/environments/environment.prod.ts` + `fileReplacements` in `angular.json`

### Backend — Fly.io
- **App name:** `poker-planning-api-frosty-current-4436`
- **URL:** https://api.poker-planning-online.site
- **Fly fallback URL:** https://poker-planning-api-frosty-current-4436.fly.dev
- **Region:** `fra` (Frankfurt)
- **VM:** 1× shared-cpu-1x, 512 MB RAM
- **Config:** `backend/fly.toml`
- **Image:** built from `backend/Dockerfile` (multi-stage .NET 10 SDK → aspnet runtime)
- **Auto-stop:** machine idles to stopped state, auto-starts on next request (~5s cold start)
- **IPs:** shared IPv4 + dedicated IPv6 (both free)
- **Deploy via GitHub Actions:** `.github/workflows/fly-deploy.yml` runs `flyctl deploy --remote-only`
- **Trigger:** push to `main` touching `backend/**` or the workflow file (also manual via "Run workflow")
- **Manual deploy fallback:** `cd backend && fly deploy`

### Postgres — Neon (durable storage)
- **Endpoint:** `ep-plain-hat-altbgnv1-pooler.c-3.eu-central-1.aws.neon.tech`
- **Pooled connection** (PgBouncer in front)
- **Database:** `neondb`
- **Schema:** `poker` (EF migrations apply on every API startup)
- **Stores:** rooms, completed rounds, participant identities

### Redis — Upstash (live room state)
- **Endpoint:** `better-thrush-126989.upstash.io:6379`
- **TLS enabled**
- **Region:** Frankfurt
- **Stores:** current round phase, votes, participant presence

## Fly secrets

Set via `fly secrets set "KEY=value"` (run from `backend/`). Double underscore `__` maps to nested config keys in .NET.

| Secret | Maps to | Notes |
|--------|---------|-------|
| `ConnectionStrings__postgres` | `ConnectionStrings:postgres` | Npgsql format, `SslMode=Require` |
| `ConnectionStrings__redis` | `ConnectionStrings:redis` | StackExchange.Redis format with `ssl=True` |
| `MediatR__LicenseKey` | `MediatR:LicenseKey` | Lucky Penny Software JWT |
| `Cors__AllowedOrigins__0` | `Cors:AllowedOrigins[0]` | Exact origins, currently `https://poker-planning-online.site` |
| `Cors__AllowedWildcardOrigins__0` | `Cors:AllowedWildcardOrigins[0]` | Optional wildcard origins; app default allows `https://*.easypokerplanning.pages.dev` for Cloudflare preview deployments |
| `Authentication__Google__ClientId` | `Authentication:Google:ClientId` | Google Cloud Console → Credentials → OAuth client ID (Web). When unset, Google sign-in is disabled and `/auth/google/login` returns 503. |
| `Authentication__Google__ClientSecret` | `Authentication:Google:ClientSecret` | Same client; secret half. Never log or commit. |
| `Email__Smtp__FromEmail` | `Email:Smtp:FromEmail` | Dedicated Gmail address used as the sender. |
| `Email__Smtp__UserName` | `Email:Smtp:UserName` | Usually the same Gmail address as `FromEmail`. |
| `Email__Smtp__Password` | `Email:Smtp:Password` | Gmail app password from the sender account. Never log or commit. |
| `Email__Smtp__FromName` | `Email:Smtp:FromName` | Optional sender display name; app default is `Easy Poker`. |

List current secrets (values hidden):
```powershell
fly secrets list
```

## Local development

Aspire AppHost stays the dev loop — Fly + Docker are prod-only.

```powershell
# Full local stack via Aspire (Postgres + Redis containers + API + dashboard)
dotnet run --project backend/src/PokerPlanning.AppHost

# Angular dev server (separate terminal)
cd frontend
npm start
```

Local secrets live in:
- **.NET user-secrets** for `PokerPlanning.Api` — `MediatR:LicenseKey`, `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret`, and `Email:Smtp:*` values set via `dotnet user-secrets`
- **`.env`** at repo root (gitignored) — `MEDIATR_LICENSE_KEY` for docker-compose

## Docker compose (local prod-like test)

```powershell
docker compose up --build
```

- Frontend → http://localhost:8080
- API → http://localhost:5080
- Postgres + Redis run as compose services (not Neon/Upstash)
- Reads `.env` for `MEDIATR_LICENSE_KEY`

Tear down:
```powershell
docker compose down
```

## Deploy flow

Both deploys are driven by GitHub Actions on push to `main`. Each side has its own workflow with path filters so a backend change never re-deploys the frontend (and vice versa).

### Backend change
```powershell
git add backend/...
git commit -m "..."
git push origin main
```
- `.github/workflows/fly-deploy.yml` runs when files under `backend/**` change
- Watch progress: https://github.com/CristianFlaviu/EasyPokerPlanning/actions
- Manual deploy fallback: `cd backend && fly deploy` (requires `flyctl` installed locally)

### Frontend change
```powershell
git add frontend/...
git commit -m "..."
git push origin main
```
- `.github/workflows/pages-deploy.yml` runs when files under `frontend/**` change
- Builds Angular, deploys via `wrangler pages deploy` to project `easypokerplanning`
- Watch: same Actions tab as above
- Manual deploy fallback: build locally (`npm run build` in `frontend/`) then `npx wrangler pages deploy dist/poker-planning-frontend/browser --project-name=easypokerplanning --branch=main`

### Manual trigger (either workflow)
- GitHub → **Actions** tab → pick `Fly Deploy` or `Pages Deploy` → **Run workflow** → branch `main` → **Run**

### Database migration
- Add EF migration: `dotnet ef migrations add <Name> --project backend/src/PokerPlanning.Infrastructure --startup-project backend/src/PokerPlanning.Infrastructure --output-dir Persistence/Migrations`
- Commit + push under `backend/**`
- `fly-deploy.yml` redeploys; migration applies automatically on startup (see `Program.cs`)

## GitHub Actions secrets

Set under https://github.com/CristianFlaviu/EasyPokerPlanning/settings/secrets/actions

| Secret | Used by | How to get |
|--------|---------|------------|
| `FLY_API_TOKEN` | `fly-deploy.yml` | Fly dashboard → avatar → **Tokens** → Create deploy token |
| `CLOUDFLARE_API_TOKEN` | `pages-deploy.yml` | Cloudflare dashboard → My Profile → API Tokens → template "Edit Cloudflare Workers" |
| `CLOUDFLARE_ACCOUNT_ID` | `pages-deploy.yml` | Cloudflare dashboard right sidebar (under any page) |

Rotate any of these by creating a new token, updating the GitHub secret, then revoking the old token at the source.

## Architecture decisions

- **Migrations on startup** — single instance, no race condition. Fine for portfolio scale.
- **No SignalR Redis backplane** — single VM means no horizontal scaling, so backplane not needed.
- **Pooled Neon endpoint** — Neon free tier sleeps after 5 min idle; pooler handles transparent wake.
- **Strict CORS** — allowed origins are the canonical Pages domain plus HTTPS subdomains of the same Pages project for preview deployments. Update `Cors__AllowedOrigins__*` for exact custom domains and `Cors__AllowedWildcardOrigins__*` only for trusted wildcard domains.
- **OpenAPI + Scalar exposed in production** — intentional for portfolio demo. Hide behind dev flag if you reuse this template for real product.
- **Cookie posture** — `pp.auth` cookie uses `SameSite=None; Secure` in all environments. Required for prod (Pages and Fly are cross-site so `Lax` would drop the cookie). Works in dev too because browsers treat `http://localhost` as potentially trustworthy, so `Secure` cookies are accepted there without HTTPS.

## Google OAuth setup (prod)

Single OAuth Client ID in Google Cloud Console can serve dev + prod. After creating the Web client (see `docs/plans/google-signin.md` §4 / §10):

1. Credentials → OAuth client → Edit.
2. Authorised JavaScript origins:
   - `http://localhost:4200` (dev)
   - `https://poker-planning-online.site` (prod)
3. Authorised redirect URIs:
   - `http://localhost:5218/auth/google/callback` (dev)
   - `https://api.poker-planning-online.site/auth/google/callback` (prod)
4. Save, then push the secrets to Fly:
   ```powershell
   cd backend
   fly secrets set "Authentication__Google__ClientId=<client-id>" "Authentication__Google__ClientSecret=<client-secret>"
   ```
   Fly restarts the VM automatically.
5. Verify: open `https://poker-planning-online.site`, click `Login`, then choose Google. Google consent → land back on Pages with avatar in app bar.

OAuth consent screen stays in **Testing** while only the owner's Google account uses it; add additional Google emails under Test users to allow other testers without going through verification.

## Gmail magic-link setup

Email login uses SMTP through a dedicated Gmail account. Enable 2-step verification on that Gmail account, create an app password, then configure the API:

```powershell
dotnet user-secrets --project backend/src/PokerPlanning.Api set "Email:Smtp:FromEmail" "<gmail-address>"
dotnet user-secrets --project backend/src/PokerPlanning.Api set "Email:Smtp:UserName" "<gmail-address>"
dotnet user-secrets --project backend/src/PokerPlanning.Api set "Email:Smtp:Password" "<gmail-app-password>"
dotnet user-secrets --project backend/src/PokerPlanning.Api set "Email:Smtp:FromName" "Easy Poker"
```

Production Fly secrets:

```powershell
cd backend
fly secrets set "Email__Smtp__FromEmail=<gmail-address>" "Email__Smtp__UserName=<gmail-address>" "Email__Smtp__Password=<gmail-app-password>" "Email__Smtp__FromName=Easy Poker"
```

## Cost

Free-tier targets across all services:

| Service | Free allowance | Risk |
|---------|----------------|------|
| Cloudflare Pages | Unlimited bandwidth, 500 builds/mo | None at this scale |
| Fly.io | Pay-as-you-go, ~$0–2/mo with auto-stop + 1 VM | Charge possible if traffic spikes |
| Neon Postgres | 0.5 GB storage, 191 compute-hr/mo | None at portfolio traffic |
| Upstash Redis | 256 MB, 500k commands/mo, 10k/day | None at portfolio traffic |

Monitor Fly cost weekly:
```powershell
fly billing show
```

## Useful commands

```powershell
# Backend (Fly — requires flyctl installed locally; CI handles normal deploys)
fly status                                          # VM state
fly logs                                            # tail logs
fly ssh console                                     # shell into VM
fly secrets list                                    # secret keys
fly scale count 1                                   # single VM (current)
fly releases                                        # release history
fly releases rollback <version>                     # revert to previous release
fly apps destroy poker-planning-api-frosty-current-4436   # nuke app

# Database
# Open Neon dashboard SQL editor or:
psql "$env:ConnectionStrings__postgres"

# Frontend
# Cloudflare dashboard → Workers & Pages → easypokerplanning → Deployments
# Or rerun the GitHub Action "Pages Deploy" from the Actions tab
```

## Where to see deploy history

| What ran | Where |
|----------|-------|
| GitHub Action workflows (both deploys) | https://github.com/CristianFlaviu/EasyPokerPlanning/actions |
| Fly release history + currently-running version | `fly releases` / Fly dashboard → app → **Monitoring** |
| Cloudflare Pages deployments + build logs | Cloudflare dashboard → **Workers & Pages** → `easypokerplanning` → **Deployments** |
| Git source-of-truth | `git log --oneline` |

## Recovery / rotate credentials

If credentials leak (e.g. pasted in chat):
1. **Neon** dashboard → Roles → reset `neondb_owner` password
2. **Upstash** dashboard → database → Details → reset password
3. **MediatR** → buy a new key on luckypennysoftware.com (or accept dev-mode warning)
4. Update Fly secrets:
   ```powershell
   fly secrets set "ConnectionStrings__postgres=..." "ConnectionStrings__redis=..."
   ```
5. Update local `.env` and `dotnet user-secrets`
