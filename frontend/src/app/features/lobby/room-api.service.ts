import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ParticipantId, ParticipantRole, Room, RoomId } from '../../domain/room';

export interface CreateRoomRequest {
  readonly name: string;
  readonly ownerDisplayName: string;
  readonly password?: string | null;
}

export interface CreateRoomResponse {
  readonly roomId: RoomId;
  readonly ownerParticipantId: ParticipantId;
}

export interface JoinRoomRequest {
  readonly displayName: string;
  readonly role: ParticipantRole;
  readonly password?: string | null;
}

export interface JoinRoomResponse {
  readonly roomId: RoomId;
  readonly participantId: ParticipantId;
}

@Injectable({ providedIn: 'root' })
export class RoomApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/rooms`;

  createRoom(req: CreateRoomRequest): Observable<CreateRoomResponse> {
    return this.http.post<CreateRoomResponse>(this.baseUrl, req);
  }

  getRoom(roomId: RoomId): Observable<Room> {
    return this.http.get<Room>(`${this.baseUrl}/${roomId}`);
  }

  joinRoom(roomId: RoomId, req: JoinRoomRequest): Observable<JoinRoomResponse> {
    return this.http.post<JoinRoomResponse>(`${this.baseUrl}/${roomId}/join`, req);
  }
}
