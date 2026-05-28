# Google Sign-In — Implementation Plan

> Standalone implementation brief. Any agent (human or AI) can pick this up cold. Read in order: this file → `docs/domain-model.md` → `docs/progress.md` → `backend/CLAUDE.md` + `frontend/CLAUDE.md`.

**Status:** approved plan, not yet implemented.
**Owner:** unassigned.
**Last updated:** 2026-05-28.

---

## 1. Context & motivation

EasyPokerPlanning today uses an **anonymous-only** identity model: a per-browser `participantId` Guid in `localStorage` identifies a user. The owner is keyed by `Room.OwnerId : ParticipantId`. Domain doc lists "Account-based identity (anonymous only)" as v1-out-of-scope (`docs/domain-model.md:137`).

The product owner wants to add **optional Google sign-in** with a future email magic-link provider. Goals (collected from the requirements interview):

- Optional sign-in alongside anonymous flow. Anonymous must continue to work for both create and join.
- Signed-in users get: stable cross-device identity, profile name + avatar from Google, persistent room ownership.
- Future provider: email magic-link (defer to Phase 3, but design so it slots into the same auth stack).

This plan splits the work into **three phases**. Only Phase 1 is in scope for the first slice. Phases 2 and 3 are outlined here so future agents have the full picture.

---

## 2. Architecture decisions

### Backend-handled OAuth + cookie session (chosen)

| Decision | Choice | Why |
|---|---|---|
| Where OAuth runs | Backend (ASP.NET Core `AddGoogle` + `AddCookie`) | One auth stack extends to email magic-link; SignalR auto-attaches cookies; httpOnly safer than localStorage JWT. |
| Session shape | httpOnly cookie, sliding 30-day expiry, SameSite=Lax (dev) | Frontend never sees the token; SignalR negotiate carries it automatically. |
| User table | New `users` table in Postgres (`poker` schema) | Mirrors existing aggregate persistence; lets us add email/password or magic-link later without re-modelling. |
| Identity linking | `Participant.UserId` (nullable Guid) added in Phase 2 — **not** Phase 1 | Phase 1 ships UX-only sign-in; Phase 2 wires persistence; keeps PRs small. |
| Frontend OAuth library | None | Full-page redirect to `/auth/google/login`. No GIS button, no `@angular/oauth-oidc`, no `gapi`. |

### Rejected alternatives

- **Frontend GIS + ID token to backend**: Forces frontend to know about Google specifically; harder to unify with future email magic-link; needs custom JWT/session anyway.
- **JWT in localStorage**: XSS-exposed; doesn't auto-attach to SignalR negotiate; requires manual refresh handling.
- **Replace anonymous flow entirely**: Explicitly rejected by product owner. Anonymous-by-default is the v1 mental model.
- **Claim existing anonymous rooms on sign-in**: Out of scope. Anonymous rooms stay anonymous; signed-in users' new rooms get persistent ownership in Phase 2.

---

## 3. Phase split

| Phase | Scope | Outcome |
|---|---|---|
| **Phase 1 (this slice)** | Auth stack end-to-end + lobby prefill + avatar in app bar | Signed-in users see their Google identity in the UI. Rooms still use anonymous `participantId`. |
| **Phase 2** | `Participant.UserId` + `Room.OwnerUserId` linkage + cross-device history | Signed-in users keep ownership and history across devices. |
| **Phase 3** | Email magic-link provider | Sign-in option that doesn't require Google. Same `User` aggregate, new `ExternalLogin` provider value `"email"`. |

The rest of this document is **Phase 1**. Phase 2 and Phase 3 sketches live at the end.

---

## 4. Phase 1 — backend changes

### 4.1 New NuGet packages

Add to [PokerPlanning.Api.csproj](../../backend/src/PokerPlanning.Api/PokerPlanning.Api.csproj):

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="10.0.*" />
```

`Microsoft.AspNetCore.Authentication.Cookies` ships in the framework — no separate package needed.

Pin the patch version to match the existing ASP.NET Core 10 patch tier already used by other Microsoft.AspNetCore.* references (see existing pins in [progress.md](../progress.md) under "Tech debt").

### 4.2 Domain — new `User` aggregate

Create folder `backend/src/PokerPlanning.Domain/Users/` mirroring `Domain/Rooms/` shape:

**`UserId.cs`** (mirror [ParticipantId.cs](../../backend/src/PokerPlanning.Domain/Participants/ParticipantId.cs)):
```csharp
namespace PokerPlanning.Domain.Users;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
```

**`ExternalLogin.cs`**:
```csharp
namespace PokerPlanning.Domain.Users;

public sealed record ExternalLogin(string Provider, string Subject);
```

**`User.cs`** — `sealed class User : AggregateRoot`:
- Properties: `UserId Id`, `string Email`, `string DisplayName`, `string? AvatarUrl`, `DateTimeOffset CreatedAt`, `DateTimeOffset LastLoginAt`.
- `IReadOnlyList<ExternalLogin> Logins` (backed by private list).
- Factory `User.Create(email, displayName, avatarUrl, login, clock)` → raises `UserRegisteredEvent`.
- `user.RecordLogin(clock)` — updates `LastLoginAt`.
- `user.UpdateProfile(displayName, avatarUrl)` — call on every sign-in so renamed Google accounts stay in sync. No event needed.
- `user.LinkExternalLogin(login)` — guards against duplicate `(Provider, Subject)`; raises `ExternalLoginLinkedEvent` (deferred to Phase 3 when a single user may have both Google + email logins).

**`UserRegisteredEvent.cs`** — implements `IDomainEvent` like existing `RoomCreatedEvent`.

**Invariants**:
- `Email` is non-empty, normalized to lowercase trim.
- `DisplayName` is 1–80 chars.
- `Logins` contains at least one entry; `(Provider, Subject)` is unique within the list.

### 4.3 Application

**New abstractions** in `backend/src/PokerPlanning.Application/Abstractions/`:

**`IUserRepository.cs`** (mirror [IRoomRepository.cs](../../backend/src/PokerPlanning.Application/Abstractions/IRoomRepository.cs)):
```csharp
public interface IUserRepository
{
    Task<User?> GetByExternalLoginAsync(string provider, string subject, CancellationToken ct);
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}
```

`SaveChangesAsync` is invoked by the existing unit-of-work seam in the handler (same pattern `CreateRoomHandler` uses).

**New features** under `backend/src/PokerPlanning.Application/Features/`:

**`SignInWithGoogle/`**:
- `SignInWithGoogleCommand.cs` — `(string GoogleSubject, string Email, string Name, string? Picture)`. Returns `Result<UserDto>`.
- `SignInWithGoogleValidator.cs` — FluentValidation: non-empty subject/email/name.
- `SignInWithGoogleHandler.cs`:
  1. Lookup `User` by `IUserRepository.GetByExternalLoginAsync("google", subject)`.
  2. If found: `user.RecordLogin(clock)`; `user.UpdateProfile(name, picture)`.
  3. If not found: `User.Create(email, name, picture, new ExternalLogin("google", subject), clock)`; `repository.AddAsync(user)`.
  4. `dbContext.SaveChangesAsync` (via unit of work).
  5. Return `Result<UserDto>` with the user data.

**`GetCurrentUser/`**:
- `GetCurrentUserQuery.cs` — `(Guid UserId)`. Returns `Result<UserDto>`.
- `GetCurrentUserHandler.cs` — repo lookup; `Error.NotFound` if missing (treat as session-stale → endpoint will sign out).

**Shared DTO** — place `UserDto` in `Application/Features/Common/UserDto.cs` or alongside the SignInWithGoogle command, matching how `RoomDto` is organized today:
```csharp
public sealed record UserDto(Guid Id, string Email, string DisplayName, string? AvatarUrl);
```

### 4.4 Infrastructure

**`UserConfiguration.cs`** in `Infrastructure/Persistence/Configurations/` — mirror [RoomConfiguration.cs](../../backend/src/PokerPlanning.Infrastructure/Persistence/Configurations/RoomConfiguration.cs):
- Table `users` in schema `"poker"`.
- PK `id : uuid` with value conversion `UserId ↔ Guid`.
- `email : text`, **unique** index (`ix_users_email`).
- `display_name : varchar(80)`, `avatar_url : text NULL`, `created_at : timestamptz`, `last_login_at : timestamptz`.
- `logins` stored as JSON text on the same row (mirror how `ModeratorIds` is serialized at `RoomConfiguration.cs`). Format: `[{"provider":"google","subject":"108…"}]`.
- For Phase 1 dedup, in-handler check is sufficient. Add a Postgres unique constraint on `(provider, subject)` in Phase 3 when multi-provider linking lands — at that point migrate `logins` out to a child table.

**`PokerPlanningDbContext.cs`** — add `public DbSet<User> Users => Set<User>();`.

**`UserRepository.cs`** in `Infrastructure/Persistence/`:
- `GetByExternalLoginAsync` — load all users where the serialized `logins` text contains the provider+subject. Naive Phase 1 implementation: `await _db.Users.ToListAsync(ct)` then filter in memory is acceptable for a learning project at expected sub-100-user scale. Document as tech debt; move to JSONB query or child table in Phase 3.
- Register in `Infrastructure/DependencyInjection.cs` alongside `IRoomRepository`.

**New EF migration** `AddUsers`:
```bash
dotnet ef migrations add AddUsers \
  --project backend/src/PokerPlanning.Infrastructure \
  --startup-project backend/src/PokerPlanning.Api \
  --output-dir Persistence/Migrations
```

Migration history schema is already `poker` (see [InitialCreate](../../backend/src/PokerPlanning.Infrastructure/Persistence/Migrations/20260516080150_InitialCreate.cs)). The Api dev startup runs `Database.MigrateAsync()` (progress.md "EF migration baseline" slice), so the migration applies on next AppHost startup.

### 4.5 Api — authentication wiring

Modify [Program.cs](../../backend/src/PokerPlanning.Api/Program.cs). Add the auth stack **before** `app.Build()`:

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "pp.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientId missing");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientSecret missing");
        options.CallbackPath = "/auth/google/callback";
        options.SaveTokens = false;
        options.Scope.Add("email");
        options.Scope.Add("profile");

        options.Events.OnTicketReceived = async ctx =>
        {
            var principal = ctx.Principal
                ?? throw new InvalidOperationException("Google ticket missing principal");
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Google ticket missing sub");
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? "";
            var name = principal.FindFirstValue(ClaimTypes.Name) ?? email;
            var picture = principal.FindFirstValue("urn:google:picture")
                ?? principal.FindFirstValue("picture");

            var mediator = ctx.HttpContext.RequestServices.GetRequiredService<IMediator>();
            var result = await mediator.Send(
                new SignInWithGoogleCommand(subject, email, name, picture),
                ctx.HttpContext.RequestAborted);

            if (result.IsFailure)
            {
                ctx.Fail(result.Error.Code);
                return;
            }

            var user = result.Value;
            var identity = new ClaimsIdentity(
                authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
            if (!string.IsNullOrEmpty(user.AvatarUrl))
                identity.AddClaim(new Claim("picture", user.AvatarUrl));

            ctx.Principal = new ClaimsPrincipal(identity);
        };
    });

builder.Services.AddAuthorization();
```

Insert `app.UseAuthentication(); app.UseAuthorization();` **between** `app.UseCors(AppCors);` and the endpoint mapping (currently at [Program.cs:68](../../backend/src/PokerPlanning.Api/Program.cs#L68)).

### 4.6 Api — `/auth/*` endpoints

Create `backend/src/PokerPlanning.Api/Endpoints/AuthEndpoints.cs` mirroring [RoomEndpoints.cs](../../backend/src/PokerPlanning.Api/Endpoints/RoomEndpoints.cs) shape:

```csharp
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapGet("/google/login", (string? returnUrl, HttpContext http) =>
        {
            var safe = ValidateReturnUrl(returnUrl, http);
            var props = new AuthenticationProperties { RedirectUri = safe };
            return Results.Challenge(props, [GoogleDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        group.MapGet("/me", async (HttpContext http, IMediator mediator) =>
        {
            var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out var userId))
                return Results.NoContent();

            var result = await mediator.Send(new GetCurrentUserQuery(userId), http.RequestAborted);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static string ValidateReturnUrl(string? returnUrl, HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return "/";
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var abs))
        {
            // Only allow redirects to configured frontend origins.
            var allowed = http.RequestServices.GetRequiredService<IConfiguration>()
                .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (allowed.Any(o => string.Equals(o, $"{abs.Scheme}://{abs.Authority}", StringComparison.OrdinalIgnoreCase)))
                return returnUrl;
            return "/";
        }
        return returnUrl.StartsWith('/') ? returnUrl : "/";
    }
}
```

Wire in `Program.cs` near the existing `app.MapRoomEndpoints();` call:
```csharp
app.MapAuthEndpoints();
```

Note: the Google handler's `CallbackPath = "/auth/google/callback"` is handled automatically — no endpoint mapping needed. The `OnTicketReceived` hook above creates the user and replaces the principal; the cookie middleware then signs the user in and redirects to `AuthenticationProperties.RedirectUri`.

### 4.7 Existing endpoints — unchanged

`RoomEndpoints` continue to resolve identity via `X-Participant-Id` (`RoomEndpoints.cs:114–124`). The auth cookie is silently present on signed-in requests but is **not yet read** by room endpoints — that's Phase 2.

`RoomHub` (`Api/Hubs/RoomHub.cs`) — unchanged. SignalR identity still flows via header/query `participantId`.

### 4.8 Configuration

Google credentials via `dotnet user-secrets` in dev:
```bash
dotnet user-secrets --project backend/src/PokerPlanning.Api set "Authentication:Google:ClientId" "<id>"
dotnet user-secrets --project backend/src/PokerPlanning.Api set "Authentication:Google:ClientSecret" "<secret>"
```

Production: Aspire parameter/env var. **Never** commit to `appsettings*.json`.

Google Cloud Console — OAuth 2.0 Client ID (Web application):
- Authorized redirect URIs:
  - `http://localhost:5218/auth/google/callback` (dev)
  - `https://<api-prod-host>/auth/google/callback` (prod)
- Authorized JavaScript origins: not required (we redirect through the backend).

CORS already permits credentials (`Program.cs:55`). Confirm the post-callback `returnUrl` host (e.g. `http://localhost:4200`) is in `Cors:AllowedOrigins`.

---

## 5. Phase 1 — frontend changes

### 5.1 No new npm packages

Use plain `fetch`/`HttpClient` and a full-page redirect. No `@angular/oauth-oidc`, no `gapi`.

### 5.2 New `core/auth/` module

`frontend/src/app/core/auth/auth.service.ts`:

```ts
import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserDto } from '../../domain/user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _currentUser = signal<UserDto | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isSignedIn = computed(() => this._currentUser() !== null);

  async refresh(): Promise<void> {
    try {
      const user = await firstValueFrom(
        this.http.get<UserDto | null>(`${environment.apiBaseUrl}/auth/me`, {
          withCredentials: true,
          observe: 'body',
        }),
      );
      this._currentUser.set(user ?? null);
    } catch {
      this._currentUser.set(null);
    }
  }

  signInWithGoogle(returnUrl: string = window.location.href): void {
    const url = `${environment.apiBaseUrl}/auth/google/login?returnUrl=${encodeURIComponent(returnUrl)}`;
    window.location.assign(url);
  }

  async signOut(): Promise<void> {
    await firstValueFrom(
      this.http.post(`${environment.apiBaseUrl}/auth/logout`, null, { withCredentials: true }),
    );
    this._currentUser.set(null);
  }
}
```

`frontend/src/app/domain/user.ts`:
```ts
export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
}
```

Add a provider in [app.config.ts](../../frontend/src/app/app.config.ts) that calls `authService.refresh()` once on app init (Angular 21 pattern — use `provideAppInitializer(() => inject(AuthService).refresh())`, or replicate the existing init pattern in the codebase).

### 5.3 HTTP `withCredentials`

Extend the existing [participant-id.interceptor.ts](../../frontend/src/app/core/http/participant-id.interceptor.ts) (or add a sibling `credentials.interceptor.ts`) to set `withCredentials: true` on requests whose URL starts with `environment.apiBaseUrl`. Without this, the browser will not attach the `pp.auth` cookie on cross-origin requests during dev (`localhost:4200` → `localhost:5218`).

### 5.4 SignalR `withCredentials`

In [signalr.service.ts](../../frontend/src/app/core/signalr/signalr.service.ts) around line 109, update the `HubConnectionBuilder().withUrl(...)` options:
```ts
.withUrl(`${environment.apiBaseUrl}/hubs/rooms?participantId=${participantId}`, {
  withCredentials: true,
})
```

ParticipantId stays in the query string — unchanged. The cookie just travels along.

### 5.5 App bar — sign-in button + user menu

Modify [shared/app-bar/app-bar.component.ts](../../frontend/src/app/shared/app-bar/app-bar.component.ts). Inject `AuthService`. Render inside the existing `<ng-content>` slot (or add a fixed slot in the component template):

- If `!auth.isSignedIn()`:
  ```html
  <button mat-stroked-button (click)="auth.signInWithGoogle()">
    <mat-icon>login</mat-icon>
    Sign in with Google
  </button>
  ```
- If `auth.isSignedIn()`:
  ```html
  <button mat-button [matMenuTriggerFor]="userMenu">
    <img [src]="auth.currentUser()!.avatarUrl ?? defaultAvatar" alt="" class="pp-avatar" />
    <span>{{ auth.currentUser()!.displayName }}</span>
  </button>
  <mat-menu #userMenu>
    <button mat-menu-item (click)="auth.signOut()">
      <mat-icon>logout</mat-icon> Sign out
    </button>
  </mat-menu>
  ```

`AppBar` is used in lobby ([lobby.page.html:1](../../frontend/src/app/features/lobby/lobby.page.html#L1)), room, and history pages — the button appears everywhere automatically.

### 5.6 Lobby prefill

In [lobby.page.ts](../../frontend/src/app/features/lobby/lobby.page.ts) around the form definition (lines 36–40), inject `AuthService` and seed `ownerDisplayName` with the current user's name:

```ts
private readonly auth = inject(AuthService);

readonly createForm = this.fb.group({
  name: ['', [Validators.required, Validators.maxLength(80)]],
  ownerDisplayName: [this.auth.currentUser()?.displayName ?? '', [Validators.required, Validators.maxLength(40)]],
  password: ['', [Validators.maxLength(80)]],
});

constructor() {
  effect(() => {
    const user = this.auth.currentUser();
    if (user && !this.createForm.controls.ownerDisplayName.dirty) {
      this.createForm.patchValue({ ownerDisplayName: user.displayName });
    }
  });
}
```

The field stays editable and required — a signed-in user can still type a different display name for this specific room. No fields become required because of sign-in. Same prefill logic applies to the join-room dialog if the design wants symmetric behaviour (out of scope for Phase 1 unless trivially achievable).

---

## 6. Phase 1 — docs to update

- **`docs/domain-model.md`** — replace line 137 "Account-based identity (anonymous only)" with: "Optional Google sign-in available; rooms remain anonymous-by-default. Signed-in identity is profile-only in Phase 1 — room/owner linkage lands in Phase 2."
- **`docs/progress.md`** — add a "Google sign-in Phase 1" entry under **Done** when implementation lands, and a Phase 2 entry under **Next** linking back to this plan.

---

## 7. Phase 1 — verification

Order matters; do not skip the regression smoke at step 5.

1. **Backend build**: `dotnet build backend/PokerPlanning.slnx`. Expect 0 errors.
2. **Migration applies cleanly on a fresh DB**:
   - Reset the local Aspire Postgres volume (`pokerplanning.apphost-*-postgres-server-data`).
   - `dotnet run --project backend/src/PokerPlanning.AppHost`.
   - Watch API log for `Applying migration '<timestamp>_AddUsers'`.
3. **Frontend build**: `npm run build` in `frontend/`.
4. **Manual sign-in smoke** (Browser MCP / Playwright — see `docs/ai-test-use-cases/browser-testing-guide.md`):
   - Open `http://localhost:4200`. App bar shows "Sign in with Google".
   - Click → Google consent screen → returned to `http://localhost:4200`.
   - App bar now shows avatar + name. DevTools → Application → Cookies confirms `pp.auth` is httpOnly + present.
   - `GET /auth/me` (DevTools Network) returns 200 + `UserDto` when signed in, 204 when signed out.
   - Open user menu → "Sign out" → app bar reverts to "Sign in".
   - **Repeat sign-in on the same Google account** → only one row appears in `users` table (verify via pgAdmin available in Aspire dashboard).
5. **Anonymous regression**: run `./scripts/smoke-test.ps1 -SkipBuild` in a fresh incognito window with **no sign-in**. All anonymous flows (create, join, vote, reveal, end, history) must pass.
6. **SignalR cookie posture**: in DevTools Network, the `/hubs/rooms` negotiate request carries `Cookie: pp.auth=...` for signed-in sessions.

---

## 8. Phase 2 — sketch (next slice, not for this PR)

### Linking signed-in identity to rooms

**Domain changes**:
- `Participant.UserId : UserId?` — nullable; set when the joining caller is authenticated.
- `Room.OwnerUserId : UserId?` — nullable; set when the creating caller is authenticated.
- New domain method `room.AddParticipant(participantId, displayName, role, password, userId)` overload that accepts the optional userId.

**Application**:
- `CreateRoomCommand` and `JoinRoomCommand` gain optional `UserId? CallerUserId`.
- Handlers persist the userId into Participant / Room.
- `GetParticipantRoomsQuery` accepts optional `UserId? CallerUserId` and the repo query becomes: rooms where any participant matches `participantId` **or** `userId` (if provided).
- New `IUserContext` abstraction in `Application/Abstractions/` exposes `Guid? CurrentUserId` — resolved from the cookie principal in an API-side `UserContext` implementation.

**Api**:
- Endpoints inject `IUserContext` (scoped). `CreateRoom` / `JoinRoom` / history endpoints pass `userContext.CurrentUserId` into the command.

**Infrastructure**:
- Migration `AddParticipantUserId` — nullable `user_id : uuid` on `room_participants` and `owner_user_id : uuid` on `rooms`. No data migration; existing rows stay NULL.
- `RoomRepository.ListByParticipantIdAsync` becomes `ListByCallerAsync(participantId, userId)` — query OR-joins on both columns.

**Frontend**:
- No UI change for the join/create flow itself.
- History page's `GET /rooms/history` request will start including the user's signed-in rooms automatically (the new server logic picks up the cookie's userId).
- Optional UX: badge "your account" next to rooms surfaced via userId rather than participantId.

**Regression watchpoints**:
- Anonymous users must continue to see only their browser's rooms.
- Signed-in user creating a room on Browser A, then signing in on Browser B, should see the room in history on B.

### What Phase 2 deliberately does **not** do

- Does **not** retroactively link existing anonymous rooms to a newly signed-up user.
- Does **not** introduce ownership transfer between users.
- Does **not** make sign-in mandatory anywhere.

---

## 9. Phase 3 — sketch (email magic-link, future)

- Same `User` aggregate. Add `ExternalLogin("email", <email>)` for users who sign up by email.
- New `AddEmailMagicLink` flow: POST `/auth/email/request` → server emits a signed short-lived token via email; GET `/auth/email/callback?token=...` validates + signs the cookie in.
- Email transport: defer choice (Mailtrap for dev, Resend/SES for prod).
- Migrate `users.logins` JSON column to a child table `user_logins(user_id, provider, subject)` with unique `(provider, subject)` constraint at this point — multi-provider linking benefits from a proper relational model and a unique index.
- Frontend: second button "Sign in with email" → form with email field → "check your inbox" confirmation.
- All Phase 1 + Phase 2 plumbing carries over unchanged.

---

## 10. Open questions for the next agent

If you pick up Phase 1 and any of these are unclear, ask before guessing:

1. **`returnUrl` allow-list source**: the plan reads `Cors:AllowedOrigins`. Confirm this matches frontend deployment hosts (`localhost:4200`, `easypokerplanning.pages.dev`, preview wildcards via `Cors:AllowedWildcardOrigins`). Cloudflare Pages preview URLs may need separate handling.
2. **`SameSite=Lax` in prod**: works as long as frontend and API are on the same eTLD+1. If the frontend stays at `easypokerplanning.pages.dev` and the API is hosted on a different domain, switch to `SameSite=None; Secure` and ensure the API is HTTPS — required for the cookie to travel cross-site.
3. **App bar slot ergonomics**: the existing `<app-bar>` uses `<ng-content>` for nav buttons. Decide whether the sign-in button lives inside the slot (each consuming page adds it) or becomes a built-in fixed slot of the component itself. Recommendation: built-in fixed slot — sign-in is global, not page-specific.
4. **Initial `auth.refresh()` failure mode**: if `/auth/me` returns 401 (cookie expired), the service should silently treat the user as signed out. Already handled by the `try/catch` in §5.2 — confirm no toast fires from the existing error interceptor for this specific 401 (consider passing an HTTP context flag to skip the global error toast for `/auth/me`).

---

## 11. File checklist for Phase 1 implementer

Backend — add:
- [ ] `backend/src/PokerPlanning.Domain/Users/UserId.cs`
- [ ] `backend/src/PokerPlanning.Domain/Users/ExternalLogin.cs`
- [ ] `backend/src/PokerPlanning.Domain/Users/User.cs`
- [ ] `backend/src/PokerPlanning.Domain/Users/UserRegisteredEvent.cs`
- [ ] `backend/src/PokerPlanning.Application/Abstractions/IUserRepository.cs`
- [ ] `backend/src/PokerPlanning.Application/Features/SignInWithGoogle/SignInWithGoogleCommand.cs`
- [ ] `backend/src/PokerPlanning.Application/Features/SignInWithGoogle/SignInWithGoogleValidator.cs`
- [ ] `backend/src/PokerPlanning.Application/Features/SignInWithGoogle/SignInWithGoogleHandler.cs`
- [ ] `backend/src/PokerPlanning.Application/Features/GetCurrentUser/GetCurrentUserQuery.cs`
- [ ] `backend/src/PokerPlanning.Application/Features/GetCurrentUser/GetCurrentUserHandler.cs`
- [ ] `backend/src/PokerPlanning.Application/Features/Common/UserDto.cs` (or per-feature)
- [ ] `backend/src/PokerPlanning.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- [ ] `backend/src/PokerPlanning.Infrastructure/Persistence/UserRepository.cs`
- [ ] `backend/src/PokerPlanning.Infrastructure/Persistence/Migrations/<timestamp>_AddUsers.cs` (generated)
- [ ] `backend/src/PokerPlanning.Api/Endpoints/AuthEndpoints.cs`

Backend — modify:
- [ ] `backend/src/PokerPlanning.Api/PokerPlanning.Api.csproj` — add `Microsoft.AspNetCore.Authentication.Google`
- [ ] `backend/src/PokerPlanning.Api/Program.cs` — auth stack + middleware ordering + `MapAuthEndpoints`
- [ ] `backend/src/PokerPlanning.Infrastructure/Persistence/PokerPlanningDbContext.cs` — `DbSet<User> Users`
- [ ] `backend/src/PokerPlanning.Infrastructure/DependencyInjection.cs` — register `IUserRepository`

Frontend — add:
- [ ] `frontend/src/app/core/auth/auth.service.ts`
- [ ] `frontend/src/app/domain/user.ts`
- [ ] (optional) `frontend/src/app/core/http/credentials.interceptor.ts`

Frontend — modify:
- [ ] `frontend/src/app/app.config.ts` — provide AuthService + init refresh + register credentials interceptor (if separate)
- [ ] `frontend/src/app/core/http/participant-id.interceptor.ts` — set `withCredentials: true` (if folding in here)
- [ ] `frontend/src/app/core/signalr/signalr.service.ts` — `withCredentials: true` on `withUrl` options
- [ ] `frontend/src/app/shared/app-bar/app-bar.component.ts` (+ HTML/SCSS) — sign-in button + avatar menu
- [ ] `frontend/src/app/features/lobby/lobby.page.ts` — prefill `ownerDisplayName` from auth signal

Docs:
- [ ] `docs/domain-model.md` — update line 137 wording
- [ ] `docs/progress.md` — Done entry on completion; Next entry referencing Phase 2
