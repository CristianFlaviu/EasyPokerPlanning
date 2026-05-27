import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Card, ParticipantId, ParticipantRole, Room, RoomId } from '../../domain/room';

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

export interface StartRoundRequest {
  readonly title?: string | null;
}

export interface StartRoundResponse {
  readonly roomId: RoomId;
  readonly roundId: string;
}

export interface ParticipantRoomsResponse {
  readonly rooms: readonly ParticipantRoomSummary[];
}

export interface ParticipantRoomSummary {
  readonly id: RoomId;
  readonly name: string;
  readonly completedRoundCount: number;
  readonly lastActiveAt: string;
}

export interface RoomHistoryResponse {
  readonly roomId: RoomId;
  readonly rounds: readonly CompletedRound[];
}

export interface CompletedRound {
  readonly id: string;
  readonly title: string | null;
  readonly votes: readonly CompletedVote[];
  readonly finalEstimate: Card | null;
  readonly startedAt: string;
  readonly endedAt: string;
}

export interface CompletedVote {
  readonly participantId: ParticipantId;
  readonly card: Card;
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

  startRound(roomId: RoomId, req: StartRoundRequest): Observable<StartRoundResponse> {
    return this.http.post<StartRoundResponse>(`${this.baseUrl}/${roomId}/rounds`, req);
  }

  submitVote(roomId: RoomId, card: Card): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${roomId}/round/vote`, { card });
  }

  revealVotes(roomId: RoomId): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${roomId}/round/reveal`, {});
  }

  resetRound(roomId: RoomId): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${roomId}/round/reset`, {});
  }

  endRound(roomId: RoomId, finalEstimate?: Card | null): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${roomId}/round/end`, { finalEstimate });
  }

  getParticipantRooms(): Observable<ParticipantRoomsResponse> {
    return this.http.get<ParticipantRoomsResponse>(`${this.baseUrl}/history`);
  }

  getRoomHistory(roomId: RoomId): Observable<RoomHistoryResponse> {
    return this.http.get<RoomHistoryResponse>(`${this.baseUrl}/${roomId}/history`);
  }

  promoteModerator(roomId: RoomId, participantId: ParticipantId): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${roomId}/moderators/${participantId}`, {});
  }

  demoteModerator(roomId: RoomId, participantId: ParticipantId): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${roomId}/moderators/${participantId}`);
  }

  changeRole(roomId: RoomId, role: ParticipantRole): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${roomId}/participants/me/role`, { role });
  }

  leaveRoom(roomId: RoomId): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${roomId}/participants/me`);
  }

  removeParticipant(roomId: RoomId, participantId: ParticipantId): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${roomId}/participants/${participantId}`);
  }
}
