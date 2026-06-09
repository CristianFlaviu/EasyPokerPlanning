import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FeedbackRequest {
  readonly message: string;
  readonly name?: string | null;
  readonly email?: string | null;
}

@Injectable({ providedIn: 'root' })
export class FeedbackService {
  private readonly http = inject(HttpClient);

  // withCredentials so a signed-in user's cookie rides along and the backend can
  // attach their UserId to the stored feedback.
  submit(request: FeedbackRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/feedback`, request, {
      withCredentials: true,
    });
  }
}
