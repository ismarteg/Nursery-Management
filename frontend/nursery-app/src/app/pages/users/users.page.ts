import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface UserRow {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  roleIds: string[];
}

@Component({
  selector: 'app-users-page',
  imports: [PageHeaderComponent, TranslatePipe, RouterLink],
  templateUrl: './users.page.html',
  styleUrl: './users.page.scss'
})
export class UsersPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly users = signal<UserRow[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  toggleActive(user: UserRow): void {
    this.http.post<{ id: string; isActive: boolean }>(
      `${environment.apiUrl}/users/${user.id}/toggle-active`,
      {}
    ).subscribe({
      next: () => this.reload(),
      error: (err) => this.onError(err)
    });
  }

  remove(user: UserRow): void {
    const confirmed = window.confirm(`Delete user "${user.userName}"? / حذف المستخدم "${user.userName}"؟`);
    if (!confirmed) {
      return;
    }

    this.http.delete(`${environment.apiUrl}/users/${user.id}`).subscribe({
      next: () => this.reload(),
      error: (err) => this.onError(err)
    });
  }

  private onError(err: { error?: { message?: string } }): void {
    this.messages.fromHttpError(err);
  }

  private reload(): void {
    this.loading.set(true);
    this.http.get<UserRow[]>(`${environment.apiUrl}/users`).subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
