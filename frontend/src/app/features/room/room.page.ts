import { HttpResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { RoomApiService } from '../lobby/room-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { SignalRService, ThrownReaction } from '../../core/signalr/signalr.service';
import { IdentityService } from '../../core/identity/identity.service';
import { RoomAccessService } from '../../core/identity/room-access.service';
import { Card, FIBONACCI_DECK, ParticipantId, ParticipantRole, Room, RoomId } from '../../domain/room';
import { AppBarComponent } from '../../shared/app-bar/app-bar.component';
import { PlayingCardComponent } from '../../shared/playing-card/playing-card.component';
import { ShareRoomDialogComponent } from './share-room-dialog.component';

interface Seat {
  readonly id: ParticipantId;
  readonly displayName: string;
  readonly initial: string;
  readonly avatarUrl: string | null;
  readonly role: string;
  readonly isOwner: boolean;
  readonly isModerator: boolean;
  readonly isSelf: boolean;
  readonly isObserver: boolean;
  readonly hasVoted: boolean;
  readonly revealedCard: Card | null;
}

interface VoteDistributionItem {
  readonly card: Card;
  readonly count: number;
  readonly percent: number;
  readonly isLeader: boolean;
}

interface RevealedStats {
  readonly avg: string;
  readonly median: string;
  readonly spread: string;
  readonly agreement: string;
  readonly mode: Card | null;
  readonly totalVotes: number;
  readonly topCards: readonly Card[];
  readonly hasTie: boolean;
  readonly hasConsensus: boolean;
  readonly resultLabel: string;
  readonly resultValue: string;
  readonly resultHelp: string;
  readonly distribution: readonly VoteDistributionItem[];
}

interface ActionCue {
  readonly icon: string;
  readonly text: string;
  readonly tone: 'neutral' | 'action' | 'success';
}

// A single emoji in flight, with start/end coordinates relative to the table container.
interface FlyingReaction {
  readonly id: string;
  readonly emoji: string;
  readonly fromX: number;
  readonly fromY: number;
  readonly toX: number;
  readonly toY: number;
}

// Fixed "throwable" palette — must mirror the server-side ReactionEmojis allow-list.
const REACTION_EMOJIS = ['🍅', '☕', '❤️', '🎉', '💩', '👏', '👍', '👀'] as const;

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
  private readonly auth = inject(AuthService);
  private readonly signalr = inject(SignalRService);
  private readonly identity = inject(IdentityService);
  private readonly roomAccess = inject(RoomAccessService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef<HTMLElement>);

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

  protected readonly room = signal<Room | null>(null);
  protected readonly exporting = signal(false);

  protected readonly reactionEmojis = REACTION_EMOJIS;
  protected readonly flyingReactions = signal<readonly FlyingReaction[]>([]);
  private lastThrowAt = 0;
  private reactionSeq = 0;

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
        avatarUrl: p.avatarUrl ?? null,
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
  protected readonly canRevealVotes = computed(
    () => this.currentRound()?.phase === 'Voting' && this.votedCount() > 0,
  );

  protected readonly actionCue = computed<ActionCue>(() => {
    const round = this.currentRound();
    const canModerate = this.canModerate();
    const ownRole = this.ownParticipant()?.role;

    if (!round) {
      return canModerate
        ? {
          icon: 'play_circle',
          text: 'Start a round when the team is ready to estimate.',
          tone: 'action',
        }
        : {
          icon: 'hourglass_empty',
          text: 'Waiting for a moderator to start the next round.',
          tone: 'neutral',
        };
    }

    if (round.phase === 'Voting') {
      if (ownRole === 'Observer') {
        return {
          icon: 'visibility',
          text: 'You are observing. Switch to Voter if you need to cast a card.',
          tone: 'neutral',
        };
      }

      if (canModerate) {
        const remaining = Math.max(this.voterCount() - this.votedCount(), 0);
        return remaining === 0 && this.voterCount() > 0
          ? {
            icon: 'task_alt',
            text: 'All voters have picked. Reveal when discussion is ready.',
            tone: 'success',
          }
          : {
            icon: 'how_to_vote',
            text:
              this.votedCount() === 0
                ? 'Waiting for the first vote before reveal is available.'
                : `${this.votedCount()} of ${this.voterCount()} voters have picked. Reveal when discussion is ready.`,
            tone: 'action',
          };
      }

      return this.ownVote()
        ? {
          icon: 'check_circle',
          text: 'Your vote is saved. You can change it until the reveal.',
          tone: 'success',
        }
        : {
          icon: 'style',
          text: 'Pick a card from your deck before the moderator reveals.',
          tone: 'action',
        };
    }

    return canModerate
      ? {
        icon: 'analytics',
        text: 'Review the distribution, then choose a final estimate or reset the vote.',
        tone: 'action',
      }
      : {
        icon: 'analytics',
        text: 'Results are revealed. Waiting for the moderator to end or reset the round.',
        tone: 'neutral',
      };
  });

  protected readonly stats = computed<RevealedStats | null>(() => {
    const round = this.currentRound();
    if (!round || round.phase !== 'Revealed') return null;
    const revealedCards = round.votes
      .map((v) => v.card)
      .filter((c): c is Card => c != null);
    const numeric = round.votes
      .map((v) => v.card)
      .filter((c): c is Card => c != null && c !== '?')
      .map((c) => parseInt(c as string, 10))
      .filter((n) => !Number.isNaN(n));

    const totalVotes = revealedCards.length;
    const counts = new Map<Card, number>(this.deck.map((card) => [card, 0]));
    revealedCards.forEach((card) => counts.set(card, (counts.get(card) ?? 0) + 1));
    const topCount = Math.max(0, ...Array.from(counts.values()));
    const topCards = this.deck.filter((card) => (counts.get(card) ?? 0) === topCount && topCount > 0);
    const hasTie = topCards.length > 1;
    const hasConsensus = totalVotes > 0 && topCards.length === 1 && topCount === totalVotes;
    const mode = topCards.length === 1 ? topCards[0] : null;
    const distribution = this.deck
      .map((card) => {
        const count = counts.get(card) ?? 0;
        return {
          card,
          count,
          percent: totalVotes === 0 ? 0 : Math.round((count / totalVotes) * 100),
          isLeader: count > 0 && count === topCount,
        };
      })
      .filter((item) => item.count > 0);

    const sorted = [...numeric].sort((a, b) => a - b);
    const sum = sorted.reduce((a, b) => a + b, 0);
    const avg = sorted.length === 0 ? '-' : (sum / sorted.length).toFixed(1);
    const mid = Math.floor(sorted.length / 2);
    const median =
      sorted.length === 0
        ? '-'
        : sorted.length % 2 === 0
          ? ((sorted[mid - 1] + sorted[mid]) / 2).toFixed(1)
          : `${sorted[mid]}`;
    const spread =
      sorted.length === 0
        ? '-'
        : sorted[0] === sorted[sorted.length - 1]
          ? `${sorted[0]}`
          : `${sorted[0]}-${sorted[sorted.length - 1]}`;

    let resultLabel = 'No votes revealed';
    let resultValue = '-';
    let resultHelp = 'There are no votes to summarize.';

    if (hasConsensus && mode) {
      resultLabel = mode === '?' ? 'Shared signal' : 'Consensus estimate';
      resultValue = mode;
      resultHelp =
        mode === '?'
          ? 'Every revealed vote asked for more information.'
          : `Every revealed vote selected ${mode}.`;
    } else if (hasTie) {
      resultLabel = 'No clear leader';
      resultValue = topCards.join(' / ');
      resultHelp = `Tie across ${topCards.length} cards. Choose a final estimate manually or reset.`;
    } else if (mode) {
      resultLabel = 'Most common estimate';
      resultValue = mode;
      resultHelp = `${topCount} of ${totalVotes} revealed votes selected ${mode}.`;
    }

    return {
      avg,
      median,
      spread,
      agreement: totalVotes === 0 ? '-' : `${topCount}/${totalVotes}`,
      mode,
      totalVotes,
      topCards,
      hasTie,
      hasConsensus,
      resultLabel,
      resultValue,
      resultHelp,
      distribution,
    };
  });

  protected readonly modeCard = computed(() => {
    const s = this.stats();
    return s?.mode != null && !s.hasTie ? s.mode : null;
  });

  constructor() {
    effect((onCleanup) => {
      const roomId = this.roomId();
      if (!roomId) {
        return;
      }

      if (this.roomAccess.getToken(roomId)) {
        this.loadRoom(roomId);
        void this.signalr.connectToRoom(roomId);
      } else if (this.auth.isSignedIn()) {
        this.restoreAccessAndLoadRoom(roomId);
      } else {
        this.loadRoom(roomId);
      }
      onCleanup(() => void this.signalr.disconnectFromRoom());
    });

    this.signalr.reactions$
      .pipe(takeUntilDestroyed())
      .subscribe((reaction) => {
        try {
          this.spawnReaction(reaction);
        } catch (err) {
          console.error('Failed to render reaction', err);
        }
      });
  }

  protected throwReaction(targetId: ParticipantId, emoji: string): void {
    const roomId = this.roomId();
    if (!roomId || targetId === this.identity.participantId) return;

    // Light client-side cooldown — loose enough to allow a little playful spam.
    const now = Date.now();
    if (now - this.lastThrowAt < 500) return;
    this.lastThrowAt = now;

    this.api.throwReaction(roomId, targetId, emoji).subscribe();
  }

  // Renders an incoming reaction as an emoji that arcs from the sender's seat to the
  // target's seat, then self-removes once the animation has played.
  private spawnReaction(reaction: ThrownReaction): void {
    const table = this.host.nativeElement.querySelector('.room__table') as HTMLElement | null;
    if (!table) return;

    const target = table.querySelector(
      `[data-pid="${reaction.toParticipantId}"] .pp-avatar`,
    ) as HTMLElement | null;
    if (!target) return;

    const sender = table.querySelector(
      `[data-pid="${reaction.fromParticipantId}"] .pp-avatar`,
    ) as HTMLElement | null;

    const base = table.getBoundingClientRect();
    const targetRect = target.getBoundingClientRect();
    const senderRect = sender?.getBoundingClientRect() ?? targetRect;

    const center = (rect: DOMRect, axis: 'x' | 'y') =>
      axis === 'x'
        ? rect.left + rect.width / 2 - base.left
        : rect.top + rect.height / 2 - base.top;

    const item: FlyingReaction = {
      id: this.nextReactionId(),
      emoji: reaction.emoji,
      fromX: center(senderRect, 'x'),
      fromY: center(senderRect, 'y'),
      toX: center(targetRect, 'x'),
      toY: center(targetRect, 'y'),
    };

    this.flyingReactions.update((list) => [...list, item]);
    setTimeout(
      () => this.flyingReactions.update((list) => list.filter((r) => r.id !== item.id)),
      1100,
    );
  }

  // Local-only unique id. Avoids crypto.randomUUID, which is undefined in insecure
  // (non-HTTPS) contexts and older browsers.
  private nextReactionId(): string {
    return `r-${Date.now()}-${this.reactionSeq++}`;
  }

  private restoreAccessAndLoadRoom(roomId: RoomId): void {
    this.api.restoreRoomAccess(roomId).subscribe({
      next: () => {
        this.loadRoom(roomId);
        void this.signalr.connectToRoom(roomId);
      },
      error: () => this.loadRoom(roomId),
    });
  }

  private loadRoom(roomId: RoomId): void {
    this.api.getRoom(roomId).subscribe((room) => {
      this.room.set(room);
      this.signalr.setParticipants(room.participants);
      this.signalr.setModeratorIds(room.moderatorIds);
      this.signalr.setCurrentRound(room.currentRound);
    });
  }

  protected startRound(): void {
    const roomId = this.roomId();
    if (!roomId) return;
    const title = this.roundTitle.value.trim();
    const roundTitle = title.length > 0 ? title : null;
    this.api
      .startRound(roomId, { title: roundTitle })
      .subscribe((response) => {
        this.signalr.setCurrentRound({
          id: response.roundId,
          title: roundTitle,
          phase: 'Voting',
          votes: [],
        });
        this.roundTitle.setValue('');
      });
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
    if (roomId && this.canRevealVotes()) this.api.revealVotes(roomId).subscribe();
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

  protected exportVotes(): void {
    const roomId = this.roomId();
    if (!roomId || this.exporting()) return;
    this.exporting.set(true);
    this.api.exportRoomVotes(roomId).subscribe({
      next: (response) => {
        this.exporting.set(false);
        const blob = response.body;
        if (blob) this.downloadBlob(blob, this.filenameFrom(response) ?? 'room-votes.csv');
      },
      // The error interceptor already surfaces a snackbar; just clear the busy flag.
      error: () => this.exporting.set(false),
    });
  }

  private filenameFrom(response: HttpResponse<Blob>): string | null {
    const disposition = response.headers.get('Content-Disposition');
    return disposition?.match(/filename="?([^"]+)"?/i)?.[1] ?? null;
  }

  private downloadBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
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
      .subscribe(() => {
        // Token is now stored; reload full room state and open the live connection.
        this.loadRoom(roomId);
        void this.signalr.connectToRoom(roomId);
      });
  }

  protected isModerator(participantId: ParticipantId): boolean {
    return this.moderatorIds().includes(participantId);
  }

  protected canManageParticipant(participantId: ParticipantId): boolean {
    return this.isOwner() || this.canRemoveParticipant(participantId);
  }

  protected canRemoveParticipant(participantId: ParticipantId): boolean {
    return (
      this.canModerate() &&
      this.room()?.ownerId !== participantId &&
      this.identity.participantId !== participantId
    );
  }

  protected promoteModerator(participantId: ParticipantId): void {
    const roomId = this.roomId();
    if (roomId) this.api.promoteModerator(roomId, participantId).subscribe();
  }

  protected demoteModerator(participantId: ParticipantId): void {
    const roomId = this.roomId();
    if (roomId) this.api.demoteModerator(roomId, participantId).subscribe();
  }

  protected removeParticipant(participantId: ParticipantId): void {
    const roomId = this.roomId();
    if (roomId && this.canRemoveParticipant(participantId)) {
      this.api.removeParticipant(roomId, participantId).subscribe();
    }
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

  protected openShareDialog(): void {
    this.dialog.open(ShareRoomDialogComponent, {
      data: {
        roomName: this.room()?.name ?? 'Planning room',
        url: window.location.href,
      },
      panelClass: 'share-room-dialog-panel',
      width: 'min(560px, calc(100vw - 32px))',
      autoFocus: 'dialog',
    });
  }

  protected isOutlier(seat: Seat): boolean {
    const stats = this.stats();
    if (!stats || !seat.revealedCard || seat.revealedCard === '?') return false;
    const val = parseInt(seat.revealedCard, 10);
    const mode = stats.mode == null || stats.mode === '?' ? Number.NaN : parseInt(stats.mode, 10);
    if (Number.isNaN(val) || Number.isNaN(mode)) return false;
    return Math.abs(val - mode) >= 5;
  }
}
