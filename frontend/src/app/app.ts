import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FeedbackFabComponent } from './shared/feedback-fab/feedback-fab.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, FeedbackFabComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('poker-planning-frontend');
}
