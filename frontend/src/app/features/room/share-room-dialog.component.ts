import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';

export interface ShareRoomDialogData {
  readonly roomName: string;
  readonly url: string;
}

@Component({
  selector: 'pp-share-room-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatInputModule],
  templateUrl: './share-room-dialog.component.html',
  styleUrl: './share-room-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShareRoomDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ShareRoomDialogComponent>);
  private readonly snack = inject(MatSnackBar);

  protected readonly data = inject<ShareRoomDialogData>(MAT_DIALOG_DATA);

  protected close(): void {
    this.dialogRef.close();
  }

  protected selectUrl(event: Event): void {
    const input = event.target as HTMLInputElement;
    input.select();
  }

  protected async copyUrl(): Promise<void> {
    try {
      if (navigator.clipboard) {
        await navigator.clipboard.writeText(this.data.url);
      } else {
        this.copyUrlFallback();
      }

      this.snack.open('Room link copied', undefined, { duration: 1800 });
    } catch {
      this.copyUrlFallback();
      this.snack.open('Room link copied', undefined, { duration: 1800 });
    }
  }

  private copyUrlFallback(): void {
    const textarea = document.createElement('textarea');
    textarea.value = this.data.url;
    textarea.setAttribute('readonly', '');
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand('copy');
    textarea.remove();
  }
}
