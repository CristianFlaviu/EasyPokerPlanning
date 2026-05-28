import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { AppBarComponent } from '../../shared/app-bar/app-bar.component';
import { FannedDeckComponent } from '../../shared/fanned-deck/fanned-deck.component';
import { JoinRoomDialogComponent } from './join-room-dialog.component';
import { RoomApiService } from './room-api.service';

@Component({
  selector: 'pp-lobby-page',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    RouterLink,
    AppBarComponent,
    FannedDeckComponent,
  ],
  templateUrl: './lobby.page.html',
  styleUrl: './lobby.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LobbyPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(RoomApiService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly auth = inject(AuthService);

  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    ownerDisplayName: [
      this.auth.currentUser()?.displayName ?? '',
      [Validators.required, Validators.maxLength(40)],
    ],
    password: [''],
  });

  constructor() {
    effect(() => {
      const user = this.auth.currentUser();
      const control = this.form.controls.ownerDisplayName;
      if (user && !control.dirty && !control.value) {
        control.setValue(user.displayName);
      }
    });
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    const { name, ownerDisplayName, password } = this.form.getRawValue();
    this.api
      .createRoom({
        name,
        ownerDisplayName,
        password: password.length > 0 ? password : null,
      })
      .subscribe({
        next: (res) => {
          this.submitting.set(false);
          this.router.navigate(['/room', res.roomId]);
        },
        error: () => this.submitting.set(false),
      });
  }

  protected openJoinDialog(): void {
    this.dialog
      .open(JoinRoomDialogComponent, {
        panelClass: 'join-room-dialog-panel',
        width: 'min(520px, calc(100vw - 32px))',
        autoFocus: 'first-tabbable',
      })
      .afterClosed()
      .subscribe((roomId?: string) => {
        if (roomId) {
          void this.router.navigate(['/room', roomId]);
        }
      });
  }
}
