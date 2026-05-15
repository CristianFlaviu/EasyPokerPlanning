import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Component({
  selector: 'pp-room-page',
  imports: [MatCardModule],
  templateUrl: './room.page.html',
  styleUrl: './room.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly roomId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: '' },
  );
}
