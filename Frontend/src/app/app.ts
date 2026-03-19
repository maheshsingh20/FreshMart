import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './shared/components/navbar/navbar';
import { ThemeService } from './core/services/theme.service';
import { AiChat } from './shared/components/ai-chat/ai-chat';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, AiChat],
  templateUrl: './app.html'
})
export class App implements OnInit {
  private theme = inject(ThemeService);
  ngOnInit() { this.theme.init(); }
}
