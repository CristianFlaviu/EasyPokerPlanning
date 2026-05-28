import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserDto } from '../../domain/user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _currentUser = signal<UserDto | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isSignedIn = computed(() => this._currentUser() !== null);

  async refresh(): Promise<void> {
    try {
      const user = await firstValueFrom(
        this.http.get<UserDto | null>(`${environment.apiBaseUrl}/auth/me`, {
          withCredentials: true,
          observe: 'body',
        }),
      );
      this._currentUser.set(user ?? null);
    } catch {
      this._currentUser.set(null);
    }
  }

  signInWithGoogle(returnUrl: string = window.location.href): void {
    const url = `${environment.apiBaseUrl}/auth/google/login?returnUrl=${encodeURIComponent(returnUrl)}`;
    window.location.assign(url);
  }

  async signOut(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post(`${environment.apiBaseUrl}/auth/logout`, null, {
          withCredentials: true,
        }),
      );
    } finally {
      this._currentUser.set(null);
    }
  }
}
