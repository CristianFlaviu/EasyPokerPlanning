import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'pp-app-bar',
  imports: [RouterLink, MatButtonModule, MatIconModule, MatMenuModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="pp-app-bar">
      <a routerLink="/" class="pp-app-bar__brand">
        <span class="logo" aria-hidden="true">EP</span>
        <span>Easy Poker</span>
      </a>
      <nav class="pp-app-bar__nav">
        <ng-content />
        @if (auth.isSignedIn()) {
          <button
            mat-button
            type="button"
            class="pp-app-bar__user"
            [matMenuTriggerFor]="userMenu"
            [attr.aria-label]="'Signed in as ' + auth.currentUser()!.displayName"
          >
            @if (auth.currentUser()!.avatarUrl) {
              <img
                [src]="auth.currentUser()!.avatarUrl"
                alt=""
                referrerpolicy="no-referrer"
                class="pp-app-bar__avatar"
              />
            } @else {
              <mat-icon class="pp-app-bar__avatar-fallback">account_circle</mat-icon>
            }
            <span class="pp-app-bar__user-name">{{ auth.currentUser()!.displayName }}</span>
          </button>
          <mat-menu #userMenu="matMenu">
            <div class="pp-app-bar__user-meta" mat-menu-item disabled>
              <span class="pp-app-bar__user-name">{{ auth.currentUser()!.displayName }}</span>
              <span class="pp-app-bar__user-email">{{ auth.currentUser()!.email }}</span>
            </div>
            <button mat-menu-item type="button" (click)="onSignOut()">
              <mat-icon>logout</mat-icon>
              <span>Sign out</span>
            </button>
          </mat-menu>
        } @else {
          <button
            mat-stroked-button
            type="button"
            (click)="onSignIn()"
            class="pp-app-bar__sign-in"
          >
            <mat-icon>login</mat-icon>
            <span>Sign in with Google</span>
          </button>
        }
      </nav>
    </header>
  `,
})
export class AppBarComponent {
  protected readonly auth = inject(AuthService);

  protected onSignIn(): void {
    this.auth.signInWithGoogle();
  }

  protected onSignOut(): void {
    void this.auth.signOut();
  }
}
