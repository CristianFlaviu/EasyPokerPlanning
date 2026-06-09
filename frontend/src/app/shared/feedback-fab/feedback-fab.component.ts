import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'pp-feedback-fab',
  imports: [MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './feedback-fab.component.html',
  styleUrl: './feedback-fab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeedbackFabComponent {
  private readonly dialog = inject(MatDialog);

  // The teaser bubble is hidden until the user taps the robot.
  protected readonly teaserOpen = signal(false);

  protected toggleTeaser(): void {
    this.teaserOpen.update((open) => !open);
  }

  protected closeTeaser(): void {
    this.teaserOpen.set(false);
  }

  // Lazy-import the dialog so its Material form modules stay out of the initial bundle
  // (this FAB is mounted eagerly in the app shell).
  protected async openDialog(): Promise<void> {
    this.closeTeaser();
    const { FeedbackDialogComponent } = await import('../../core/feedback/feedback-dialog.component');
    this.dialog.open(FeedbackDialogComponent, {
      panelClass: 'feedback-dialog-panel',
      width: 'min(560px, calc(100vw - 32px))',
      autoFocus: 'dialog',
    });
  }
}
