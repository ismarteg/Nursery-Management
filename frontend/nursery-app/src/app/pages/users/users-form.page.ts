import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface RoleOption {
  id: string;
  name: string;
  description?: string;
}

interface UserRow {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roleIds: string[];
}

interface UserForm {
  userName: string;
  fullName: string;
  email: string;
  password: string;
  isActive: boolean;
  roleIds: string[];
}

@Component({
  selector: 'app-users-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './users-form.page.html',
  styleUrl: './users-form.page.scss'
})
export class UsersFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly roles = signal<RoleOption[]>([]);
  readonly editingId = signal<string | null>(null);

  form: UserForm = this.emptyForm();

  ngOnInit(): void {
    this.http.get<RoleOption[]>(`${environment.apiUrl}/roles`).subscribe({
      next: (roles) => this.roles.set(roles)
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.editingId.set(id);
    this.http.get<UserRow>(`${environment.apiUrl}/users/${id}`).subscribe({
      next: (user) => {
        this.form = {
          userName: user.userName,
          fullName: user.fullName,
          email: user.email,
          password: '',
          isActive: user.isActive,
          roleIds: [...(user.roleIds ?? [])]
        };
        this.loading.set(false);
      },
      error: () => {
        this.messages.errorKey('MSG.NOT_FOUND');
        this.loading.set(false);
      }
    });
  }

  toggleRole(roleId: string, checked: boolean): void {
    if (checked) {
      if (!this.form.roleIds.includes(roleId)) {
        this.form.roleIds = [...this.form.roleIds, roleId];
      }
      return;
    }
    this.form.roleIds = this.form.roleIds.filter((id) => id !== roleId);
  }

  isRoleSelected(roleId: string): boolean {
    return this.form.roleIds.includes(roleId);
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    const id = this.editingId();

    const request$ = id
      ? this.http.put(`${environment.apiUrl}/users/${id}`, {
          fullName: this.form.fullName,
          email: this.form.email,
          isActive: this.form.isActive,
          roleIds: this.form.roleIds
        })
      : this.http.post(`${environment.apiUrl}/users`, {
          userName: this.form.userName,
          fullName: this.form.fullName,
          email: this.form.email,
          password: this.form.password,
          isActive: this.form.isActive,
          roleIds: this.form.roleIds
        });

    request$.subscribe({
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

  private emptyForm(): UserForm {
    return {
      userName: '',
      fullName: '',
      email: '',
      password: '',
      isActive: true,
      roleIds: []
    };
  }
}
