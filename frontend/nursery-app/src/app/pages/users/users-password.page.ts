import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface UserRow {
  id: string;
  userName: string;
  fullName: string;
}

@Component({
  selector: 'app-users-password-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './users-password.page.html',
  styleUrl: './users-password.page.scss'
})
export class UsersPasswordPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly userId = signal<string | null>(null);
  readonly userName = signal<string | null>(null);

  newPassword = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.messages.errorKey('MSG.NOT_FOUND');
      this.loading.set(false);
      return;
    }

    this.userId.set(id);
    this.http.get<UserRow>(`${environment.apiUrl}/users/${id}`).subscribe({
      next: (user) => {
        this.userName.set(`${user.fullName} (${user.userName})`);
        this.loading.set(false);
      },
      error: () => {
        this.messages.errorKey('MSG.NOT_FOUND');
        this.loading.set(false);
      }
    });
  }

  save(): void {
    const id = this.userId();
    if (!id || !this.newPassword) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.http
      .post(`${environment.apiUrl}/users/${id}/reset-password`, {
        newPassword: this.newPassword
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          void this.router.navigate(['/users']);
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
}
