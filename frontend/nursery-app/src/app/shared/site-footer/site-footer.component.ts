import { Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-site-footer',
  imports: [TranslatePipe],
  templateUrl: './site-footer.component.html',
  styleUrl: './site-footer.component.scss'
})
export class SiteFooterComponent {
  readonly year = new Date().getFullYear();
}
