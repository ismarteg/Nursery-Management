import { Component, HostListener, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { MessageService } from '../../core/messages/message.service';

@Component({
  selector: 'app-message-modal',
  imports: [TranslatePipe],
  templateUrl: './message-modal.component.html',
  styleUrl: './message-modal.component.scss'
})
export class MessageModalComponent {
  readonly messages = inject(MessageService);

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.messages.current()) {
      this.messages.close();
    }
  }
}
