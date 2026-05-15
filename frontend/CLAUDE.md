# Frontend Agent Instructions (Angular 21)

> Loaded when working inside `frontend/`. Read the root `CLAUDE.md` and `docs/domain-model.md` first.

## Critical: Angular 21 ≠ older Angular
Angular 21 was released November 2025. If your training data is older or mixed, the safest assumption is **everything you remember about Angular is potentially outdated**. Verify against `https://angular.dev` (the new docs site) before generating non-trivial code.

Specifically:
- **No `NgModule`.** Standalone components only. App is bootstrapped with `bootstrapApplication(AppComponent, appConfig)`.
- **Zoneless change detection is default.** No Zone.js. If something doesn't re-render, the value isn't in a signal.
- **New control flow:** `@if`, `@for`, `@switch`, `@let` in templates. No structural directives.
- **Signal inputs/outputs:** `input()`, `input.required()`, `output()`, `model()`. No `@Input()` / `@Output()` decorators.
- **`inject()`** over constructor injection in most cases.
- **`provideHttpClient()`** in `app.config.ts`; interceptors are functions, not classes.
- **Vitest** is the default test runner. Karma/Jasmine config is not present.

## Project structure
```
frontend/src/app/
├── app.config.ts            # providers, router, http, signalr config
├── app.component.ts         # root standalone component
├── app.routes.ts            # lazy-loaded route definitions
├── core/                    # singletons: services, interceptors, guards
│   ├── signalr/             # connection management, hub proxy
│   ├── identity/            # participantId generation/storage
│   ├── http/                # interceptors (error, participant-id)
│   └── theme/               # Material theme config
├── shared/                  # reusable presentational components, pipes, directives
├── features/                # one folder per feature route
│   ├── lobby/               # create / join room screen
│   ├── room/                # the active voting room
│   └── history/             # past rooms list + detail
└── domain/                  # TS types mirroring backend DTOs (hand-maintained or codegen)
```

## State management approach
- **Signals first.** No NgRx, no Akita, no BehaviorSubjects for component state.
- For shared state across components within a feature, use a `@Injectable({ providedIn: 'root' })` service that exposes signals (`readonly votes = signal<Vote[]>([])`).
- For derived state: `computed()`.
- For side effects: `effect()` — but use sparingly. Most logic belongs in event handlers.
- For HTTP streams: still RxJS. Convert at boundaries with `toSignal()`.
- For SignalR streams: the `SignalRService` exposes signals derived from internal RxJS subjects.

## Component conventions
Every component is standalone:
```typescript
@Component({
  selector: 'pp-room-card',
  imports: [MatCardModule, MatButtonModule],
  templateUrl: './room-card.component.html',
  styleUrl: './room-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomCardComponent {
  readonly room = input.required<Room>();
  readonly selected = input(false);
  readonly cardSelected = output<Card>();

  private readonly router = inject(Router);

  protected readonly displayName = computed(() => this.room().name.toUpperCase());

  protected onPick(card: Card): void {
    this.cardSelected.emit(card);
  }
}
```

Rules:
- `OnPush` change detection on every component
- `readonly` on all signal inputs/outputs
- `protected` for template-accessed members, `private` otherwise
- Selector prefix: `pp-` (poker planning)
- One component per file, file name matches class

## Routing
- All routes are lazy-loaded via `loadComponent`:
  ```typescript
  export const routes: Routes = [
    { path: '', loadComponent: () => import('./features/lobby/lobby.page').then(m => m.LobbyPage) },
    { path: 'room/:id', loadComponent: () => import('./features/room/room.page').then(m => m.RoomPage) },
    { path: 'history', loadComponent: () => import('./features/history/history.page').then(m => m.HistoryPage) },
  ];
  ```
- Route components are named `*.page.ts` to distinguish from presentational components.

## Angular Material 3 conventions
Material 3 is the current theming system in Angular 21. The Sass mixin patterns from M2 are largely replaced by CSS custom properties and the system token API.

- Theme is defined once in `core/theme/_theme.scss` using `mat.theme(...)` (Material 3 mixin)
- Components consume theme via CSS custom properties: `var(--mat-sys-primary)`, etc.
- **Do not import Material modules globally.** Each component imports only what it uses (`MatButtonModule`, etc.) in its `imports` array
- Prefer Material's typography tokens over custom font sizing
- Use `MatSnackBar` for transient notifications, `MatDialog` for confirmation flows

If you generate Material code using older mixin patterns (`@include mat.core-theme($theme)` etc.), stop and verify against current Material 3 docs.

## SignalR integration
A single `SignalRService` in `core/signalr/`:
- Owns the `HubConnection` lifecycle (connect on login / room entry, disconnect on leave)
- Exposes a signal-based API for state derived from server events:
  - `participants = signal<Participant[]>([])`
  - `currentRound = signal<Round | null>(null)`
  - `connectionState = signal<'connecting' | 'connected' | 'disconnected'>('disconnected')`
- Provides typed methods for invoking hub methods: `submitVote(card)`, `reveal()`, etc.
- Handles automatic reconnection and re-joining the room group

Components subscribe to these signals; they never touch the hub directly.

## HTTP & error handling
- One global error interceptor that maps backend `Result.Failure` responses to user-visible toasts and to a re-thrown error
- One participant-id interceptor that attaches the `X-Participant-Id` header from `IdentityService`
- Use `HttpResource` (Angular 21 feature) for declarative read-only queries where it fits; plain `HttpClient` for everything else

## Forms
- Reactive forms only (no template-driven forms)
- **Do not use Signal Forms** — still developer preview as of Angular 21.x, not production-ready
- Validators inline; custom validators in `core/validators/` if reused

## Performance defaults
- All components OnPush
- Track functions on `@for` blocks: `@for (p of participants(); track p.id) { ... }`
- Defer non-critical UI: `@defer (on viewport) { <pp-history-panel /> }`
- Use `signal()` and `computed()` over manual subscription patterns

## Forbidden patterns
- `NgModule`, `*ngIf`, `*ngFor`, `*ngSwitch`
- `@Input()`, `@Output()` decorators (use `input()`, `output()`)
- `ChangeDetectorRef.markForCheck()` calls — if you need this, your state isn't a signal
- Importing `BrowserModule` or `CommonModule` (they're not needed with standalone + new control flow)
- Subscribing to observables in templates with `subscribe()` — use `| async` or `toSignal()`
- Class-based HTTP interceptors — use functional ones
- Global Material module imports (`MaterialModule` barrel files)
- Karma/Jasmine references in tests (we use Vitest if/when needed)
- Signal Forms (`@angular/forms/signals`) — preview only

## When unsure
- Angular API: `https://angular.dev/api/...`
- Material 3 theming: `https://material.angular.dev` (the new docs site for Material)
- If a pattern looks like older Angular (5-16 era), double-check before using it
