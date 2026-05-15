import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ParticipantId, RoomId } from '../../domain/room';

export interface CreateRoomRequest {
  readonly name: string;
  readonly ownerDisplayName: string;
  readonly password?: string | null;
}

export interface CreateRoomResponse {
  readonly roomId: RoomId;
  readonly ownerParticipantId: ParticipantId;
}

@Injectable({ providedIn: 'root' })
export class RoomApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/rooms`;

  createRoom(req: CreateRoomRequest): Observable<CreateRoomResponse> {
    return this.http.post<CreateRoomResponse>(this.baseUrl, req);
  }
}
