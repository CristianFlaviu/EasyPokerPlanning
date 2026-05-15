import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, switchMap } from 'rxjs';
import { RoomApiService } from '../lobby/room-api.service';

@Component({
  selector: 'pp-room-page',
  imports: [MatCardModule, MatListModule],
  templateUrl: './room.page.html',
  styleUrl: './room.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomPage {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(RoomApiService);

  protected readonly roomId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: '' },
  );

  protected readonly room = toSignal(
    this.route.paramMap.pipe(
      map((p) => p.get('id') ?? ''),
      filter((id) => id.length > 0),
      switchMap((id) => this.api.getRoom(id)),
    ),
  );
}
