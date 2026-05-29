import { Injectable } from '@angular/core';
import { RoomId } from '../../domain/room';

const KEY_PREFIX = 'pp.roomToken.';

/**
 * Stores the server-signed per-room seat token returned by create/join.
 * The token is the credential for all room actions, reads, and live updates.
 */
@Injectable({ providedIn: 'root' })
export class RoomAccessService {
  getToken(roomId: RoomId): string | null {
    return localStorage.getItem(KEY_PREFIX + roomId);
  }

  setToken(roomId: RoomId, token: string): void {
    localStorage.setItem(KEY_PREFIX + roomId, token);
  }

  clearToken(roomId: RoomId): void {
    localStorage.removeItem(KEY_PREFIX + roomId);
  }
}
