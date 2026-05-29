import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { UserDto } from '../../domain/user';
import { RoomAccessService } from '../identity/room-access.service';

export interface EmailLoginRequest {
  readonly mode: 'login' | 'signup';
  readonly email: string;
  readonly displayName?: string | null;
  readonly returnUrl: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly roomAccess = inject(RoomAccessService);
  private readonly router = inject(Router);
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

  uploadAvatar(file: File): Observable<{ readonly avatarUrl: string }> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<{ readonly avatarUrl: string }>(
      `${environment.apiBaseUrl}/auth/me/avatar`,
      formData,
      { withCredentials: true },
    );
  }

  updateProfile(displayName: string, avatarUrl: string | null): Observable<UserDto> {
    return this.http
      .put<UserDto>(
        `${environment.apiBaseUrl}/auth/me/profile`,
        { displayName, avatarUrl },
        { withCredentials: true },
      )
      .pipe(tap((user) => this._currentUser.set(user)));
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
      this.roomAccess.clearAllTokens();
      window.dispatchEvent(new Event('pp:signout'));
      if (this.router.url.startsWith('/room/')) {
        await this.router.navigate(['/']);
      }
    }
  }
}
