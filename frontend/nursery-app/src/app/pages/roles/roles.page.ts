import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface RoleRow {
  id: string;
  name: string;
  description?: string;
  permissions: string[];
  permissionIds: string[];
  usersCount: number;
  isSystem: boolean;
}

@Component({
  selector: 'app-roles-page',
  imports: [PageHeaderComponent, TranslatePipe, RouterLink],
  templateUrl: './roles.page.html',
  styleUrl: './roles.page.scss'
})
export class RolesPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly roles = signal<RoleRow[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  remove(role: RoleRow): void {
    if (role.isSystem) {
      return;
    }

    const confirmed = window.confirm(
      `Delete role "${role.name}"? / حذف الدور "${role.name}"؟`
    );
    if (!confirmed) {
      return;
    }

    this.http.delete(`${environment.apiUrl}/roles/${role.id}`).subscribe({
      next: () => this.reload(),
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.http.get<RoleRow[]>(`${environment.apiUrl}/roles`).subscribe({
      next: (roles) => {
        this.roles.set(roles);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
