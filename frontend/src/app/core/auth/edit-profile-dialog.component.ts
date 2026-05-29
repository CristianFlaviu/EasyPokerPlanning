import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs/operators';
import { AuthService } from './auth.service';

const MAX_AVATAR_BYTES = 5 * 1024 * 1024;
const ALLOWED_AVATAR_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

@Component({
  selector: 'pp-edit-profile-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './edit-profile-dialog.component.html',
  styleUrl: './edit-profile-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditProfileDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly dialogRef = inject(MatDialogRef<EditProfileDialogComponent>);

  protected readonly accept = ALLOWED_AVATAR_TYPES.join(',');
  protected readonly submitting = signal(false);
  protected readonly fileError = signal<string | null>(null);
  protected readonly previewUrl = signal<string | null>(this.auth.currentUser()?.avatarUrl ?? null);

  private selectedFile: File | null = null;
  private objectUrl: string | null = null;

  protected readonly form = this.fb.nonNullable.group({
    displayName: [
      this.auth.currentUser()?.displayName ?? '',
      [Validators.required, Validators.maxLength(80)],
    ],
  });

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    if (!ALLOWED_AVATAR_TYPES.includes(file.type)) {
      this.fileError.set('Choose a JPEG, PNG, or WebP image.');
      return;
    }

    if (file.size > MAX_AVATAR_BYTES) {
      this.fileError.set('Image must be at most 5 MB.');
      return;
    }

    this.fileError.set(null);
    this.selectedFile = file;
    this.revokeObjectUrl();
    this.objectUrl = URL.createObjectURL(file);
    this.previewUrl.set(this.objectUrl);
  }

  protected close(): void {
    this.dialogRef.close();
  }

  protected save(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const displayName = this.form.getRawValue().displayName.trim();
    const currentAvatar = this.auth.currentUser()?.avatarUrl ?? null;
    this.submitting.set(true);

    if (this.selectedFile) {
      this.auth.uploadAvatar(this.selectedFile).subscribe({
        next: ({ avatarUrl }) => this.submitProfile(displayName, avatarUrl),
        error: () => this.submitting.set(false),
      });
      return;
    }

    this.submitProfile(displayName, currentAvatar);
  }

  private submitProfile(displayName: string, avatarUrl: string | null): void {
    this.auth
      .updateProfile(displayName, avatarUrl)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.revokeObjectUrl();
          this.dialogRef.close();
        },
      });
  }

  private revokeObjectUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
  }
}
