import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserDto } from '../../domain/user';

export interface EmailLoginRequest {
  readonly mode: 'login' | 'signup';
  readonly email: string;
  readonly displayName?: string | null;
  readonly returnUrl: string;
}

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

  requestEmailLogin(request: EmailLoginRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/email/request`, request, {
      withCredentials: true,
    });
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
