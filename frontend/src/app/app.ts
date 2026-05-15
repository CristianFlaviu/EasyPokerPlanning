import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppFooterComponent } from './shared/app-footer/app-footer.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AppFooterComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('poker-planning-frontend');
}
