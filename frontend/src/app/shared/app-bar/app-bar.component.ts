import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'pp-app-bar',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="pp-app-bar">
      <a routerLink="/" class="pp-app-bar__brand">
        <span class="logo" aria-hidden="true">EP</span>
        <span>Easy Poker</span>
        
      </a>
      <nav class="pp-app-bar__nav">
        <ng-content />
      </nav>
    </header>
  `,
})
export class AppBarComponent { }
