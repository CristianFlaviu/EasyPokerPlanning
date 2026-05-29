import { Injectable, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { IdentityService } from '../identity/identity.service';
import { CurrentRound, Participant, RoomId, Vote } from '../../domain/room';

type ConnectionState = 'connecting' | 'connected' | 'disconnected';

interface ParticipantJoinedMessage {
  readonly id: string;
  readonly displayName: string;
  readonly role: Participant['role'];
  readonly avatarUrl: string | null;
}

interface ParticipantLeftMessage {
  readonly participantId: string;
}

interface ModeratorChangedMessage {
  readonly participantId: string;
}

interface ParticipantRoleChangedMessage {
  readonly participantId: string;
  readonly role: Participant['role'];
}

interface ParticipantProfileChangedMessage {
  readonly participantId: string;
  readonly displayName: string;
  readonly avatarUrl: string | null;
}

interface RoundStartedMessage {
  readonly id: string;
  readonly title: string | null;
  readonly phase: CurrentRound['phase'];
}

interface VoteSubmittedMessage {
  readonly roundId: string;
  readonly participantId: string;
}

interface VotesRevealedMessage {
  readonly roundId: string;
  readonly votes: readonly RevealedVoteMessage[];
}

interface RevealedVoteMessage {
  readonly participantId: string;
  readonly card: Vote['card'];
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly identity = inject(IdentityService);
  private connection: HubConnection | null = null;
  private activeRoomId: RoomId | null = null;

  readonly participants = signal<readonly Participant[]>([]);
  readonly moderatorIds = signal<readonly string[]>([]);
  readonly currentRound = signal<CurrentRound | null>(null);
  readonly connectionState = signal<ConnectionState>('disconnected');

  setParticipants(participants: readonly Participant[]): void {
    this.participants.set(participants);
  }

  setModeratorIds(moderatorIds: readonly string[]): void {
    this.moderatorIds.set(moderatorIds);
  }

  setCurrentRound(round: CurrentRound | null): void {
    this.currentRound.set(round);
  }

  recordOwnVote(participantId: string, card: Vote['card']): void {
    this.currentRound.update((round) => {
      if (!round) {
        return round;
      }

      return {
        ...round,
        votes: this.upsertVote(round.votes, {
          participantId,
          card,
          isRevealed: round.phase === 'Revealed',
        }),
      };
    });
  }

  async connectToRoom(roomId: RoomId): Promise<void> {
    if (
      this.activeRoomId === roomId &&
      this.connection?.state === HubConnectionState.Connected
    ) {
      return;
    }

    await this.disconnectFromRoom();

    this.activeRoomId = roomId;
    this.connectionState.set('connecting');

    this.connection = new HubConnectionBuilder()
      .withUrl(
        `${environment.apiBaseUrl}/hubs/rooms?participantId=${this.identity.participantId}`,
        { withCredentials: true },
      )
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.connection.onreconnecting(() => this.connectionState.set('connecting'));
    this.connection.onreconnected(() => {
      this.connectionState.set('connected');
      void this.joinActiveRoom();
    });
    this.connection.onclose(() => this.connectionState.set('disconnected'));

    this.connection.on('ParticipantJoined', (message: ParticipantJoinedMessage) => {
      this.upsertParticipant({
        id: message.id,
        displayName: message.displayName,
        role: message.role,
        avatarUrl: message.avatarUrl,
      });
    });

    this.connection.on('ParticipantLeft', (message: ParticipantLeftMessage) => {
      this.participants.update((participants) =>
        participants.filter((participant) => participant.id !== message.participantId),
      );
      this.moderatorIds.update((ids) => ids.filter((id) => id !== message.participantId));
      this.currentRound.update((round) =>
        round
          ? {
              ...round,
              votes: round.votes.filter((vote) => vote.participantId !== message.participantId),
            }
          : round,
      );
    });

    this.connection.on('RoundStarted', (message: RoundStartedMessage) => {
      this.currentRound.set({
        id: message.id,
        title: message.title,
        phase: message.phase,
        votes: [],
      });
    });

    this.connection.on('VoteSubmitted', (message: VoteSubmittedMessage) => {
      this.currentRound.update((round) => {
        if (!round || round.id !== message.roundId) {
          return round;
        }

        return {
          ...round,
          votes: this.upsertVote(round.votes, {
            participantId: message.participantId,
            card: null,
            isRevealed: false,
          }),
        };
      });
    });

    this.connection.on('VotesRevealed', (message: VotesRevealedMessage) => {
      this.currentRound.update((round) => {
        if (!round || round.id !== message.roundId) {
          return round;
        }

        return {
          ...round,
          phase: 'Revealed',
          votes: message.votes.map((vote) => ({
            participantId: vote.participantId,
            card: vote.card,
            isRevealed: true,
          })),
        };
      });
    });

    this.connection.on('RoundReset', () => {
      this.currentRound.update((round) =>
        round ? { ...round, phase: 'Voting', votes: [] } : round,
      );
    });

    this.connection.on('RoundEnded', () => {
      this.currentRound.set(null);
    });

    this.connection.on('ModeratorPromoted', (message: ModeratorChangedMessage) => {
      this.moderatorIds.update((ids) =>
        ids.includes(message.participantId) ? ids : [...ids, message.participantId],
      );
    });

    this.connection.on('ModeratorDemoted', (message: ModeratorChangedMessage) => {
      this.moderatorIds.update((ids) => ids.filter((id) => id !== message.participantId));
    });

    this.connection.on('ParticipantRoleChanged', (message: ParticipantRoleChangedMessage) => {
      this.participants.update((participants) =>
        participants.map((participant) =>
          participant.id === message.participantId
            ? { ...participant, role: message.role }
            : participant,
        ),
      );
    });

    this.connection.on('ParticipantProfileChanged', (message: ParticipantProfileChangedMessage) => {
      this.participants.update((participants) =>
        participants.map((participant) =>
          participant.id === message.participantId
            ? { ...participant, displayName: message.displayName, avatarUrl: message.avatarUrl }
            : participant,
        ),
      );
    });

    await this.connection.start();
    this.connectionState.set('connected');
    await this.joinActiveRoom();
  }

  async disconnectFromRoom(): Promise<void> {
    if (!this.connection) {
      this.activeRoomId = null;
      this.connectionState.set('disconnected');
      return;
    }

    const roomId = this.activeRoomId;
    const connection = this.connection;
    this.connection = null;
    this.activeRoomId = null;

    if (roomId && connection.state === HubConnectionState.Connected) {
      await connection.invoke('LeaveRoomGroup', roomId);
    }

    await connection.stop();
    this.connectionState.set('disconnected');
  }

  async rejoinActiveRoomGroup(): Promise<void> {
    await this.joinActiveRoom();
  }

  private async joinActiveRoom(): Promise<void> {
    if (!this.activeRoomId || this.connection?.state !== HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('JoinRoomGroup', this.activeRoomId);
  }

  private upsertParticipant(participant: Participant): void {
    this.participants.update((participants) => {
      const existingIndex = participants.findIndex((p) => p.id === participant.id);
      if (existingIndex === -1) {
        return [...participants, participant];
      }

      return participants.map((p, index) =>
        index === existingIndex ? participant : p,
      );
    });
  }

  private upsertVote(votes: readonly Vote[], vote: Vote): readonly Vote[] {
    const existingIndex = votes.findIndex((v) => v.participantId === vote.participantId);
    if (existingIndex === -1) {
      return [...votes, vote];
    }

    return votes.map((existing, index) =>
      index === existingIndex ? vote : existing,
    );
  }
}
