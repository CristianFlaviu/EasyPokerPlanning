import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../auth/auth.service';
import { FeedbackService } from './feedback.service';

@Component({
  selector: 'pp-feedback-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './feedback-dialog.component.html',
  styleUrl: './feedback-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeedbackDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly feedback = inject(FeedbackService);
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<FeedbackDialogComponent>);

  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.maxLength(80)]],
    // Prefilled for signed-in users; still editable.
    email: [this.auth.currentUser()?.email ?? '', [Validators.email, Validators.maxLength(320)]],
    message: ['', [Validators.required, Validators.maxLength(4000)]],
  });

  protected close(): void {
    this.dialogRef.close();
  }

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, email, message } = this.form.getRawValue();
    this.submitting.set(true);
    this.feedback
      .submit({
        message: message.trim(),
        name: name.trim() || null,
        email: email.trim() || null,
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.snackBar.open('Thanks for the feedback! 🙌', 'Close', { duration: 4000 });
          this.dialogRef.close(true);
        },
        // The error interceptor already surfaces a snackbar; just clear the busy flag.
        error: () => this.submitting.set(false),
      });
  }
}
