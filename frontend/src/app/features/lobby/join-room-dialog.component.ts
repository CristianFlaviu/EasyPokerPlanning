import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'pp-join-room-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './join-room-dialog.component.html',
  styleUrl: './join-room-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JoinRoomDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<JoinRoomDialogComponent>);
  private readonly roomLinkValidator = (control: AbstractControl): ValidationErrors | null => {
    const value = typeof control.value === 'string' ? control.value.trim() : '';
    return value.length === 0 || this.parseRoomId(value) ? null : { roomLink: true };
  };

  protected readonly form = this.fb.nonNullable.group({
    roomLink: ['', [Validators.required, this.roomLinkValidator]],
  });

  protected close(): void {
    this.dialogRef.close();
  }

  protected join(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const roomId = this.parseRoomId(this.form.getRawValue().roomLink);
    if (!roomId) {
      this.form.controls.roomLink.setErrors({ roomLink: true });
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close(roomId);
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
