import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { PlayingCardComponent } from '../playing-card/playing-card.component';

@Component({
  selector: 'pp-fanned-deck',
  imports: [PlayingCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pp-fanned-deck" [style.height.px]="height()">
      @for (item of fan(); track item.value; let i = $index) {
        <div
          class="fanned-slot"
          [style.--fan-transform]="item.transform"
          [style.--fan-hover-transform]="item.hoverTransform"
          [style.zIndex]="i"
          [style.animationDelay.ms]="i * 60"
        >
          <pp-playing-card [value]="item.value" [selected]="item.value === selected()" />
        </div>
      }
    </div>
  `,
  styles: `
    :host { display: block; }
    .fanned-slot {
      position: absolute;
      bottom: 0;
      left: 50%;
      transform-origin: 50% 130%;
      transform: var(--fan-transform);
      transition: transform 360ms cubic-bezier(0.34, 1.56, 0.64, 1);
      animation: pp-fan-in 700ms cubic-bezier(0.22, 1, 0.36, 1) both;
    }
    .fanned-slot:hover {
      z-index: 99 !important;
      transform: var(--fan-hover-transform) !important;
    }

    @keyframes pp-fan-in {
      from {
        opacity: 0;
        transform: var(--fan-transform) translateY(24px) scale(0.94);
      }
      60% {
        opacity: 1;
        transform: var(--fan-transform) translateY(-6px) scale(1.02);
      }
      to {
        opacity: 1;
        transform: var(--fan-transform);
      }
    }
  `,
})
export class FannedDeckComponent {
  readonly cards = input<readonly string[]>(['1', '2', '3', '5', '8', '13', '21', '?']);
  readonly selected = input<string | null>(null);
  readonly spread = input(8);
  readonly height = input(190);
  readonly radius = input(420);

  protected readonly fan = computed(() => {
    const cards = this.cards();
    const spread = this.spread();
    const n = cards.length;
    const start = -((n - 1) * spread) / 2;
    return cards.map((value, i) => {
      const angle = start + i * spread;
      return {
        value,
        transform: `translateX(-50%) rotate(${angle}deg) translateY(-${this.radius() * 0.04}px)`,
        hoverTransform: `translateX(-50%) rotate(${angle}deg) translateY(-${this.radius() * 0.09}px) scale(1.04)`,
      };
    });
  });
}
