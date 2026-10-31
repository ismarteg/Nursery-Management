import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/i18n/language.service';
import { SiteFooterComponent } from '../../shared/site-footer/site-footer.component';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule, TranslatePipe, SiteFooterComponent],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss'
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly language = inject(LanguageService);

  userName = 'admin';
  password = 'Admin@123';
  readonly loading = signal(false);
  readonly error = signal(false);

  submit(): void {
    this.loading.set(true);
    this.error.set(false);
    this.auth.login(this.userName, this.password).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl('/');
      },
      error: () => {
        this.loading.set(false);
        this.error.set(true);
      }
    });
  }
}
