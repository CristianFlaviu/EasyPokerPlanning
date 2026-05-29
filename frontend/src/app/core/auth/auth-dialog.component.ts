import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { AuthService, EmailLoginRequest } from './auth.service';

export interface AuthDialogData {
  readonly mode: 'login' | 'signup';
}

@Component({
  selector: 'pp-auth-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './auth-dialog.component.html',
  styleUrl: './auth-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly dialogRef = inject(MatDialogRef<AuthDialogComponent>);
  private readonly data = inject<AuthDialogData>(MAT_DIALOG_DATA);

  protected readonly mode = signal<AuthDialogData['mode']>(this.data.mode);
  protected readonly submitting = signal(false);
  protected readonly sentTo = signal<string | null>(null);
  protected readonly resent = signal(false);
  private readonly lastRequest = signal<EmailLoginRequest | null>(null);

  protected readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
  });

  protected readonly signupForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
  });

  protected switchMode(mode: AuthDialogData['mode']): void {
    this.mode.set(mode);
    this.sentTo.set(null);
    this.resent.set(false);
    this.lastRequest.set(null);
  }

  protected close(): void {
    this.dialogRef.close();
  }

  protected continueWithGoogle(): void {
    this.auth.signInWithGoogle(window.location.href);
  }

  protected submitEmail(): void {
    const form = this.mode() === 'login' ? this.loginForm : this.signupForm;
    if (form.invalid || this.submitting()) {
      form.markAllAsTouched();
      return;
    }

    const request = this.buildRequest();
    this.sendEmailRequest(request, false);
  }

  protected resendEmail(): void {
    const request = this.lastRequest();
    if (!request || this.submitting()) {
      return;
    }

    this.sendEmailRequest(request, true);
  }

  protected editEmail(): void {
    this.sentTo.set(null);
    this.resent.set(false);
  }

  private sendEmailRequest(request: EmailLoginRequest, resent: boolean): void {
    this.submitting.set(true);
    this.auth.requestEmailLogin(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.lastRequest.set(request);
        this.sentTo.set(request.email);
        this.resent.set(resent);
      },
      error: () => this.submitting.set(false),
    });
  }

  private buildRequest(): EmailLoginRequest {
    if (this.mode() === 'login') {
      const value = this.loginForm.getRawValue();
      return {
        mode: 'login',
        email: value.email.trim(),
        returnUrl: window.location.href,
      };
    }

    const value = this.signupForm.getRawValue();
    return {
      mode: 'signup',
      email: value.email.trim(),
      returnUrl: window.location.href,
    };
  }
}
