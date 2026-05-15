export type ParticipantId = string;
export type RoomId = string;

export const FIBONACCI_DECK = ['1', '2', '3', '5', '8', '13', '21', '?'] as const;
export type Card = (typeof FIBONACCI_DECK)[number];

export type ParticipantRole = 'Voter' | 'Observer';

export interface Participant {
  readonly id: ParticipantId;
  readonly displayName: string;
  readonly role: ParticipantRole;
}

export interface Room {
  readonly id: RoomId;
  readonly name: string;
  readonly ownerId: ParticipantId;
  readonly isPasswordProtected: boolean;
  readonly participants: readonly Participant[];
}
