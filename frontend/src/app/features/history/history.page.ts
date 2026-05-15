import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import {
  CompletedRound,
  ParticipantRoomSummary,
  RoomApiService,
} from '../lobby/room-api.service';
import { RoomId } from '../../domain/room';

@Component({
  selector: 'pp-history-page',
  imports: [RouterLink, MatButtonModule, MatCardModule, MatListModule],
  templateUrl: './history.page.html',
  styleUrl: './history.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryPage {
  private readonly api = inject(RoomApiService);

  protected readonly rooms = signal<readonly ParticipantRoomSummary[]>([]);
  protected readonly selectedRoomId = signal<RoomId | null>(null);
  protected readonly rounds = signal<readonly CompletedRound[]>([]);

  constructor() {
    this.api.getParticipantRooms().subscribe((res) => this.rooms.set(res.rooms));
  }

  protected selectRoom(roomId: RoomId): void {
    this.selectedRoomId.set(roomId);
    this.api.getRoomHistory(roomId).subscribe((res) => this.rounds.set(res.rounds));
  }
}
