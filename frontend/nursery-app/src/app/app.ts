import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LanguageService } from './core/i18n/language.service';
import { MessageModalComponent } from './shared/message-modal/message-modal.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MessageModalComponent],
  template: '<router-outlet /><app-message-modal />',
  styles: ':host { display: block; min-height: 100vh; }'
})
export class App implements OnInit {
  private readonly language = inject(LanguageService);

  ngOnInit(): void {
    this.language.init();
  }
}
