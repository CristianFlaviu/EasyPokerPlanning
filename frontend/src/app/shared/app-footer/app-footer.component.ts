import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'pp-app-footer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer class="pp-app-footer" aria-label="App footer">
      <div class="pp-app-footer__inner">
        <section class="pp-app-footer__brand-panel" aria-label="Easy Poker">
          <div class="pp-app-footer__brand">
            <span class="pp-app-footer__mark" aria-hidden="true">EP</span>
            <span>
              <strong>Easy Poker</strong>
              <small>Planning room toolkit</small>
            </span>
          </div>

          <p class="pp-app-footer__copy">
            Lightweight estimation rooms for teams that need fast, anonymous Fibonacci voting.
          </p>
        </section>

        <div class="pp-app-footer__meta" aria-label="App highlights">
          <span>Hidden votes</span>
          <span>Live reveal</span>
          <span>Round history</span>
        </div>

        <div class="pp-app-footer__actions">
          <p class="pp-app-footer__legal">2026 Easy Poker</p>
        </div>
      </div>
    </footer>
  `,
  styles: `
    .pp-app-footer {
      margin-top: auto;
      color: var(--pp-ink-soft);
      width: 100%;
      border-top: 1px solid oklch(78% 0.05 250 / 0.52);
      background:
        linear-gradient(135deg, oklch(100% 0 0 / 0.72), oklch(94% 0.03 245 / 0.7)),
        linear-gradient(135deg, oklch(72% 0.13 210 / 0.14), oklch(58% 0.17 246 / 0.08));
      box-shadow: inset 0 1px 0 oklch(100% 0 0 / 0.84);
      backdrop-filter: saturate(150%) blur(14px);
    }

    .pp-app-footer__inner {
      width: 100%;
      padding: 24px clamp(24px, 4vw, 56px);
      display: grid;
      grid-template-columns: minmax(300px, 1.4fr) minmax(380px, 1fr) minmax(130px, 0.45fr);
      align-items: center;
      gap: clamp(24px, 5vw, 72px);
    }

    .pp-app-footer__brand-panel {
      min-width: 0;
      display: grid;
      gap: 10px;
    }

    .pp-app-footer__brand {
      display: inline-flex;
      align-items: flex-start;
      gap: 12px;
      min-width: 0;
      color: var(--pp-ink);
      width: fit-content;
    }

    .pp-app-footer__brand strong,
    .pp-app-footer__brand small {
      display: block;
      letter-spacing: 0;
    }

    .pp-app-footer__brand strong {
      font-size: 14px;
      font-weight: 800;
      line-height: 1.2;
    }

    .pp-app-footer__brand small {
      margin-top: 2px;
      color: var(--pp-ink-soft);
      font-size: 12px;
      font-weight: 600;
    }

    .pp-app-footer__mark {
      flex: 0 0 auto;
      width: 34px;
      height: 34px;
      border-radius: 8px;
      display: inline-grid;
      place-items: center;
      background:
        linear-gradient(140deg, var(--pp-cyan), var(--pp-accent) 48%, var(--pp-accent-deep));
      color: white;
      font-size: 12px;
      font-weight: 800;
      box-shadow: 0 10px 24px oklch(58% 0.17 246 / 0.26);
    }

    .pp-app-footer__copy {
      margin: 0;
      min-width: 0;
      font-size: 13px;
      line-height: 1.5;
      max-width: 680px;
    }

    .pp-app-footer__meta {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      align-items: center;
      gap: 10px;
      width: 100%;
    }

    .pp-app-footer__meta span {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-height: 30px;
      padding: 6px 11px;
      border: 1px solid oklch(82% 0.04 250 / 0.86);
      border-radius: var(--pp-radius-pill);
      background: oklch(100% 0 0 / 0.58);
      color: var(--pp-accent-deep);
      font-size: 12px;
      font-weight: 700;
      white-space: nowrap;
    }

    .pp-app-footer__meta span::before {
      content: '';
      width: 6px;
      height: 6px;
      margin-right: 7px;
      border-radius: 50%;
      background: var(--pp-success);
      box-shadow: 0 0 0 3px oklch(63% 0.15 155 / 0.14);
    }

    .pp-app-footer__actions {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      justify-self: end;
    }

    .pp-app-footer__legal {
      margin: 0;
      color: var(--pp-ink-soft);
      font-size: 12px;
      font-weight: 600;
      white-space: nowrap;
    }

    @media (max-width: 900px) {
      .pp-app-footer__inner {
        grid-template-columns: 1fr auto;
        gap: 14px 24px;
        padding-inline: 24px;
      }

      .pp-app-footer__meta {
        grid-column: 1 / -1;
        grid-row: 2;
      }
    }

    @media (max-width: 620px) {
      .pp-app-footer__inner {
        grid-template-columns: 1fr;
        padding: 20px 16px;
      }

      .pp-app-footer__meta {
        grid-template-columns: 1fr;
      }

      .pp-app-footer__actions {
        justify-self: start;
        justify-content: flex-start;
      }

      .pp-app-footer__meta span {
        justify-content: center;
      }
    }
  `,
})
export class AppFooterComponent {}
