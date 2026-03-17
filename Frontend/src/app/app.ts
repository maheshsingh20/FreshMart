import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './shared/components/navbar/navbar';
import { ThemeService } from './core/services/theme.service';
import { AiChat } from './shared/components/ai-chat/ai-chat';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, AiChat],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 transition-colors duration-200">
      <app-navbar />
      <router-outlet />
      <app-ai-chat />
    </div>
  `
})
export class App implements OnInit {
  private theme = inject(ThemeService);
  ngOnInit() { this.theme.init(); }
}
