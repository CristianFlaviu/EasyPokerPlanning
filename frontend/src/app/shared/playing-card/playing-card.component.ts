import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

export type PlayingCardSize = 'sm' | 'mini' | 'micro';

@Component({
  selector: 'pp-playing-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="pp-card"
      [class.pp-card--selected]="selected()"
      [class.pp-card--ghost]="ghost()"
      [class.pp-card--qmark]="value() === '?' && !selected()"
      [class.pp-card--back]="back()"
      [class.pp-card--disabled]="disabled()"
      [class.pp-card--outlier]="outlier()"
      [class.pp-card--mini]="size() === 'mini'"
      [class.pp-card--micro]="size() === 'micro'"
      [class.pp-card--flip-in]="flipIn()"
      [attr.data-value]="value()"
      [attr.aria-pressed]="selected()"
      [attr.aria-label]="ariaLabel()"
      [disabled]="disabled() || back()"
      (click)="picked.emit(value())"
    >
      @if (!back()) { {{ value() }} }
    </button>
  `,
})
export class PlayingCardComponent {
  readonly value = input.required<string>();
  readonly selected = input(false);
  readonly ghost = input(false);
  readonly back = input(false);
  readonly disabled = input(false);
  readonly outlier = input(false);
  readonly size = input<PlayingCardSize | null>(null);
  readonly flipIn = input(false);

  readonly picked = output<string>();

  protected readonly ariaLabel = computed(() =>
    this.back() ? 'Card face down' : `Card ${this.value()}`,
  );
}
