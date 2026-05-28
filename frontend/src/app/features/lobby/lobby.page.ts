import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AppBarComponent } from '../../shared/app-bar/app-bar.component';
import { FannedDeckComponent } from '../../shared/fanned-deck/fanned-deck.component';
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
  private readonly roomLinkValidator = (control: AbstractControl): ValidationErrors | null => {
    const value = typeof control.value === 'string' ? control.value.trim() : '';
    return value.length === 0 || this.parseRoomId(value) ? null : { roomLink: true };
  };

  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    ownerDisplayName: ['', [Validators.required, Validators.maxLength(40)]],
    password: [''],
  });

  protected readonly joinForm = this.fb.nonNullable.group({
    roomLink: ['', [Validators.required, this.roomLinkValidator]],
  });

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

  protected joinByLink(): void {
    if (this.joinForm.invalid) {
      this.joinForm.markAllAsTouched();
      return;
    }

    const roomId = this.parseRoomId(this.joinForm.getRawValue().roomLink);
    if (!roomId) {
      this.joinForm.controls.roomLink.setErrors({ roomLink: true });
      return;
    }

    void this.router.navigate(['/room', roomId]);
  }

  private parseRoomId(value: string): string | null {
    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }

    const fromUrl = this.parseRoomIdFromUrl(trimmed);
    return fromUrl ?? this.normalizeRoomId(trimmed);
  }

  private parseRoomIdFromUrl(value: string): string | null {
    try {
      const url = new URL(value, window.location.origin);
      const segments = url.pathname.split('/').filter(Boolean);
      const roomSegmentIndex = segments.findIndex((segment) => segment.toLowerCase() === 'room');
      if (roomSegmentIndex < 0) {
        return null;
      }

      return this.normalizeRoomId(segments[roomSegmentIndex + 1] ?? '');
    } catch {
      return null;
    }
  }

  private normalizeRoomId(value: string): string | null {
    const candidate = decodeURIComponent(value)
      .trim()
      .replace(/^\/+|\/+$/g, '');
    return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(
      candidate,
    )
      ? candidate
      : null;
  }
}
