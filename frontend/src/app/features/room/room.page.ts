import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, switchMap, tap } from 'rxjs';
import { RoomApiService } from '../lobby/room-api.service';
import { SignalRService } from '../../core/signalr/signalr.service';
import { IdentityService } from '../../core/identity/identity.service';
import { Card, FIBONACCI_DECK, ParticipantId, ParticipantRole } from '../../domain/room';
import { AppBarComponent } from '../../shared/app-bar/app-bar.component';
import { PlayingCardComponent } from '../../shared/playing-card/playing-card.component';

interface Seat {
  readonly id: ParticipantId;
  readonly displayName: string;
  readonly initial: string;
  readonly role: string;
  readonly isOwner: boolean;
  readonly isModerator: boolean;
  readonly isSelf: boolean;
  readonly isObserver: boolean;
  readonly hasVoted: boolean;
  readonly revealedCard: Card | null;
}

@Component({
  selector: 'pp-room-page',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatTooltipModule,
    RouterLink,
    AppBarComponent,
    PlayingCardComponent,
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
  private readonly snack = inject(MatSnackBar);
  private readonly router = inject(Router);

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

  protected readonly ownVote = computed<Card | null>(() => {
    const v = this.currentRound()?.votes.find(
      (vote) => vote.participantId === this.identity.participantId,
    );
    return (v?.card as Card | null | undefined) ?? null;
  });

  protected readonly isRevealed = computed(() => this.currentRound()?.phase === 'Revealed');

  protected readonly seats = computed<readonly Seat[]>(() => {
    const room = this.room();
    if (!room) return [];
    const mods = this.moderatorIds();
    const round = this.currentRound();
    const selfId = this.identity.participantId;

    return this.participants().map((p) => {
      const isOwner = room.ownerId === p.id;
      const isMod = mods.includes(p.id);
      const vote = round?.votes.find((v) => v.participantId === p.id);
      const revealedCard =
        round?.phase === 'Revealed' && vote?.card ? (vote.card as Card) : null;
      const role = isOwner ? 'OWNER' : isMod ? 'MOD' : p.role === 'Observer' ? 'OBSV' : 'VOTER';
      return {
        id: p.id,
        displayName: p.displayName,
        initial: p.displayName.charAt(0).toUpperCase() || '?',
        role,
        isOwner,
        isModerator: isMod,
        isSelf: p.id === selfId,
        isObserver: p.role === 'Observer',
        hasVoted: vote != null,
        revealedCard,
      };
    });
  });

  protected readonly topSeats = computed(() =>
    this.seats().filter((_, i) => i % 2 === 0),
  );
  protected readonly bottomSeats = computed(() =>
    this.seats().filter((_, i) => i % 2 === 1),
  );

  protected readonly votedCount = computed(
    () => this.votedParticipantIds().size,
  );
  protected readonly voterCount = computed(
    () => this.participants().filter((p) => p.role === 'Voter').length,
  );

  protected readonly stats = computed(() => {
    const round = this.currentRound();
    if (!round || round.phase !== 'Revealed') return null;
    const numeric = round.votes
      .map((v) => v.card)
      .filter((c): c is Card => c != null && c !== '?')
      .map((c) => parseInt(c as string, 10))
      .filter((n) => !Number.isNaN(n));

    if (numeric.length === 0) {
      return { avg: '-', median: '-', spread: '-', consensus: '-', mode: null };
    }

    const sorted = [...numeric].sort((a, b) => a - b);
    const sum = sorted.reduce((a, b) => a + b, 0);
    const avg = (sum / sorted.length).toFixed(1);
    const mid = Math.floor(sorted.length / 2);
    const median =
      sorted.length % 2 === 0 ? ((sorted[mid - 1] + sorted[mid]) / 2).toFixed(1) : `${sorted[mid]}`;
    const spread = sorted[0] === sorted[sorted.length - 1]
      ? `${sorted[0]}`
      : `${sorted[0]}-${sorted[sorted.length - 1]}`;

    const counts = new Map<number, number>();
    sorted.forEach((n) => counts.set(n, (counts.get(n) ?? 0) + 1));
    let mode = sorted[0];
    let modeCount = 0;
    counts.forEach((c, n) => {
      if (c > modeCount) { modeCount = c; mode = n; }
    });
    const consensus = `${modeCount}/${sorted.length}`;
    return { avg, median, spread, consensus, mode };
  });

  protected readonly modeCard = computed(() => {
    const s = this.stats();
    return s?.mode != null ? String(s.mode) : null;
  });

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
    if (!roomId) return;
    const title = this.roundTitle.value.trim();
    this.api
      .startRound(roomId, { title: title.length > 0 ? title : null })
      .subscribe(() => this.roundTitle.setValue(''));
  }

  protected submitVote(card: Card): void {
    const roomId = this.roomId();
    if (!roomId) return;
    this.api
      .submitVote(roomId, card)
      .subscribe(() => this.signalr.recordOwnVote(this.identity.participantId, card));
  }

  protected revealVotes(): void {
    const roomId = this.roomId();
    if (roomId) this.api.revealVotes(roomId).subscribe();
  }

  protected resetRound(): void {
    const roomId = this.roomId();
    if (roomId) this.api.resetRound(roomId).subscribe();
  }

  protected endRound(): void {
    const roomId = this.roomId();
    if (roomId) this.api.endRound(roomId).subscribe();
  }

  protected endRoundWithEstimate(card: Card): void {
    const roomId = this.roomId();
    if (roomId) this.api.endRound(roomId, card).subscribe();
  }

  protected joinRoom(): void {
    const roomId = this.roomId();
    if (!roomId || this.joinForm.invalid) return;
    const { displayName, password } = this.joinForm.getRawValue();
    this.api
      .joinRoom(roomId, {
        displayName,
        role: 'Voter',
        password: password.length > 0 ? password : null,
      })
      .subscribe();
  }

  protected isModerator(participantId: ParticipantId): boolean {
    return this.moderatorIds().includes(participantId);
  }

  protected promoteModerator(participantId: ParticipantId): void {
    const roomId = this.roomId();
    if (roomId) this.api.promoteModerator(roomId, participantId).subscribe();
  }

  protected demoteModerator(participantId: ParticipantId): void {
    const roomId = this.roomId();
    if (roomId) this.api.demoteModerator(roomId, participantId).subscribe();
  }

  protected changeOwnRole(role: ParticipantRole): void {
    const roomId = this.roomId();
    if (roomId) this.api.changeRole(roomId, role).subscribe();
  }

  protected leaveRoom(): void {
    const roomId = this.roomId();
    if (!roomId || this.isOwner()) return;

    this.api.leaveRoom(roomId).subscribe({
      next: () => {
        void this.signalr.disconnectFromRoom();
        void this.router.navigate(['/history']);
      },
    });
  }

  protected copyShareLink(): void {
    const url = window.location.href;
    void navigator.clipboard?.writeText(url).then(() =>
      this.snack.open('Room link copied', undefined, { duration: 1800 }),
    );
  }

  protected isOutlier(seat: Seat): boolean {
    const stats = this.stats();
    if (!stats || !seat.revealedCard || seat.revealedCard === '?') return false;
    const val = parseInt(seat.revealedCard, 10);
    if (Number.isNaN(val) || stats.mode == null) return false;
    return Math.abs(val - stats.mode) >= 5;
  }
}
