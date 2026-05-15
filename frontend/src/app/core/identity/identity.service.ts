import { Injectable } from '@angular/core';
import { ParticipantId } from '../../domain/room';

const STORAGE_KEY = 'pp.participantId';

@Injectable({ providedIn: 'root' })
export class IdentityService {
  readonly participantId: ParticipantId = this.loadOrCreate();

  private loadOrCreate(): ParticipantId {
    const existing = localStorage.getItem(STORAGE_KEY);
    if (existing) {
      return existing;
    }
    const fresh = crypto.randomUUID();
    localStorage.setItem(STORAGE_KEY, fresh);
    return fresh;
  }
}
