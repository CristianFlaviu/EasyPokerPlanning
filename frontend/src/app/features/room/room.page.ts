import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, switchMap, tap } from 'rxjs';
import { RoomApiService } from '../lobby/room-api.service';
import { SignalRService } from '../../core/signalr/signalr.service';
import { IdentityService } from '../../core/identity/identity.service';
import { Card, FIBONACCI_DECK, ParticipantId, ParticipantRole } from '../../domain/room';

@Component({
  selector: 'pp-room-page',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatListModule,
  ],
  templateUrl: './room.page.html',
  styleUrl: './room.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomPage {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(RoomApiService);
  private readonly signalr = inject(SignalRService);
  private readonly identity = inject(IdentityService);
  private readonly fb = inject(FormBuilder);

  protected readonly roomId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: '' },
  );

  protected readonly participants = this.signalr.participants;
  protected readonly moderatorIds = this.signalr.moderatorIds;
  protected readonly currentRound = this.signalr.currentRound;
  protected readonly connectionState = this.signalr.connectionState;
  protected readonly deck = FIBONACCI_DECK;
  protected readonly roundTitle = new FormControl('', { nonNullable: true });
  protected readonly joinForm = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(40)]],
    password: [''],
  });

  protected readonly room = toSignal(
    this.route.paramMap.pipe(
      map((p) => p.get('id') ?? ''),
      filter((id) => id.length > 0),
      switchMap((id) =>
        this.api.getRoom(id).pipe(
          tap((room) => {
            this.signalr.setParticipants(room.participants);
            this.signalr.setModeratorIds(room.moderatorIds);
            this.signalr.setCurrentRound(room.currentRound);
          }),
        ),
      ),
    ),
  );

  protected readonly canModerate = computed(
    () =>
      this.room()?.ownerId === this.identity.participantId ||
      this.moderatorIds().includes(this.identity.participantId),
  );

  protected readonly isOwner = computed(
    () => this.room()?.ownerId === this.identity.participantId,
  );

  protected readonly ownParticipant = computed(
    () => this.participants().find((p) => p.id === this.identity.participantId) ?? null,
  );

  protected readonly isParticipant = computed(
    () => this.participants().some((p) => p.id === this.identity.participantId),
  );

  protected readonly votedParticipantIds = computed(
    () => new Set(this.currentRound()?.votes.map((vote) => vote.participantId) ?? []),
  );

  constructor() {
    effect((onCleanup) => {
      const roomId = this.roomId();
      if (!roomId) {
        return;
      }

      void this.signalr.connectToRoom(roomId);
      onCleanup(() => void this.signalr.disconnectFromRoom());
    });
  }

  protected startRound(): void {
    const roomId = this.roomId();
    if (!roomId) {
      return;
    }

    const title = this.roundTitle.value.trim();
    this.api
      .startRound(roomId, { title: title.length > 0 ? title : null })
      .subscribe(() => this.roundTitle.setValue(''));
  }

  protected submitVote(card: Card): void {
    const roomId = this.roomId();
    if (!roomId) {
      return;
    }

    this.api
      .submitVote(roomId, card)
      .subscribe(() => this.signalr.recordOwnVote(this.identity.participantId, card));
  }

  protected revealVotes(): void {
    const roomId = this.roomId();
    if (roomId) {
      this.api.revealVotes(roomId).subscribe();
    }
  }

  protected resetRound(): void {
    const roomId = this.roomId();
    if (roomId) {
      this.api.resetRound(roomId).subscribe();
    }
  }

  protected endRound(): void {
    const roomId = this.roomId();
    if (roomId) {
      this.api.endRound(roomId).subscribe();
    }
  }

  protected endRoundWithEstimate(card: Card): void {
    const roomId = this.roomId();
    if (roomId) {
      this.api.endRound(roomId, card).subscribe();
    }
  }

  protected joinRoom(): void {
    const roomId = this.roomId();
    if (!roomId || this.joinForm.invalid) {
      return;
    }

    const { displayName, password } = this.joinForm.getRawValue();
    this.api
      .joinRoom(roomId, {
        displayName,
        role: 'Voter',
        password: password.length > 0 ? password : null,
      })
      .subscribe();
  }

  protected voteDisplay(participantId: string): string {
    const vote = this.currentRound()?.votes.find((v) => v.participantId === participantId);
    if (!vote) {
      return '';
    }

    if (vote.isRevealed && vote.card) {
      return ` - ${vote.card}`;
    }

    if (participantId === this.identity.participantId && vote.card) {
      return ` - ${vote.card}`;
    }

    return ' - voted';
  }

  protected isModerator(participantId: ParticipantId): boolean {
    return this.moderatorIds().includes(participantId);
  }

  protected promoteModerator(participantId: ParticipantId): void {
    const roomId = this.roomId();
    if (roomId) {
      this.api.promoteModerator(roomId, participantId).subscribe();
    }
  }

  protected demoteModerator(participantId: ParticipantId): void {
    const roomId = this.roomId();
    if (roomId) {
      this.api.demoteModerator(roomId, participantId).subscribe();
    }
  }

  protected changeOwnRole(role: ParticipantRole): void {
    const roomId = this.roomId();
    if (roomId) {
      this.api.changeRole(roomId, role).subscribe();
    }
  }
}
