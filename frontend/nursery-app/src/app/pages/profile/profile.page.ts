import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';

type ProfileTab = 'personal' | 'security';

interface ProfileData {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string | null;
  roles: string[];
}

@Component({
  selector: 'app-profile-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss'
})
export class ProfilePage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly profile = signal<ProfileData | null>(null);
  readonly tab = signal<ProfileTab>('personal');
  readonly error = signal<string | null>(null);

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  ngOnInit(): void {
    this.load();
  }

  setTab(tab: ProfileTab): void {
    this.tab.set(tab);
    this.error.set(null);
  }

  changePassword(): void {
    this.error.set(null);

    if (!this.currentPassword || !this.newPassword) {
      return;
    }

    if (this.newPassword.length < 6) {
      this.error.set('PROFILE.PASSWORD_MIN');
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.error.set('PROFILE.PASSWORD_MISMATCH');
      return;
    }

    this.saving.set(true);
    this.http
      .post(`${environment.apiUrl}/profile/change-password`, {
        currentPassword: this.currentPassword,
        newPassword: this.newPassword
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.currentPassword = '';
          this.newPassword = '';
          this.confirmPassword = '';
          this.messages.successKey('PROFILE.PASSWORD_CHANGED');
        },
        error: (err) => {
          this.saving.set(false);
          if (!this.messages.fromHttpError(err)) {
            this.error.set(this.messages.validationText(err));
          } else {
            this.error.set(null);
          }
        }
      });
  }

  private load(): void {
    this.loading.set(true);
    this.http.get<ProfileData>(`${environment.apiUrl}/profile`).subscribe({
      next: (data) => {
        this.profile.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.messages.fromHttpError(err);
        this.loading.set(false);
      }
    });
  }
}
