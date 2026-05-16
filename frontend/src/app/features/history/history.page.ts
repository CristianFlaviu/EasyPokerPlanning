import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import {
  CompletedRound,
  ParticipantRoomSummary,
  RoomApiService,
} from '../lobby/room-api.service';
import { RoomId } from '../../domain/room';
import { AppBarComponent } from '../../shared/app-bar/app-bar.component';
import { PlayingCardComponent } from '../../shared/playing-card/playing-card.component';

@Component({
  selector: 'pp-history-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    AppBarComponent,
    PlayingCardComponent,
  ],
  templateUrl: './history.page.html',
  styleUrl: './history.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryPage {
  private readonly api = inject(RoomApiService);

  protected readonly rooms = signal<readonly ParticipantRoomSummary[]>([]);
  protected readonly selectedRoomId = signal<RoomId | null>(null);
  protected readonly rounds = signal<readonly CompletedRound[]>([]);

  protected readonly selectedRoom = computed(
    () => this.rooms().find((r) => r.id === this.selectedRoomId()) ?? null,
  );

  constructor() {
    this.api.getParticipantRooms().subscribe((res) => {
      this.rooms.set(res.rooms);
      if (res.rooms.length > 0) {
        this.selectRoom(res.rooms[0].id);
      }
    });
  }

  protected selectRoom(roomId: RoomId): void {
    this.selectedRoomId.set(roomId);
    this.api.getRoomHistory(roomId).subscribe((res) => this.rounds.set(res.rounds));
  }

  protected shortRoomId(roomId: RoomId): string {
    return roomId.length > 14 ? `${roomId.slice(0, 8)}...${roomId.slice(-4)}` : roomId;
  }
}
