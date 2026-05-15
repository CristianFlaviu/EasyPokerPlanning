import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { RoomApiService } from './room-api.service';

@Component({
  selector: 'pp-lobby-page',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './lobby.page.html',
  styleUrl: './lobby.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LobbyPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(RoomApiService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    ownerDisplayName: ['', [Validators.required, Validators.maxLength(40)]],
    password: [''],
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
}
