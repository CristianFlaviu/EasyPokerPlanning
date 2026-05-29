import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { AuthDialogComponent, AuthDialogData } from '../../core/auth/auth-dialog.component';

@Component({
  selector: 'pp-app-bar',
  imports: [RouterLink, MatButtonModule, MatDialogModule, MatIconModule, MatMenuModule],
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
            mat-button
            type="button"
            (click)="openAuthDialog('signup')"
            class="pp-app-bar__auth-link"
          >
            Sign Up
          </button>
          <button
            mat-button
            type="button"
            (click)="openAuthDialog('login')"
            class="pp-app-bar__auth-link"
          >
            Login
          </button>
        }
      </nav>
    </header>
  `,
})
export class AppBarComponent {
  protected readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  protected openAuthDialog(mode: AuthDialogData['mode']): void {
    this.dialog.open(AuthDialogComponent, {
      panelClass: 'auth-dialog-panel',
      width: 'min(460px, calc(100vw - 32px))',
      autoFocus: 'first-tabbable',
      data: { mode },
    });
  }

  protected onSignOut(): void {
    void this.auth.signOut();
  }
}
