import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface PermissionOption {
  id: string;
  code: string;
  name: string;
  module?: string;
}

interface RoleRow {
  id: string;
  name: string;
  description?: string;
  permissionIds: string[];
  isSystem: boolean;
}

interface RoleForm {
  name: string;
  description: string;
  permissionIds: string[];
}

@Component({
  selector: 'app-roles-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './roles-form.page.html',
  styleUrl: './roles-form.page.scss'
})
export class RolesFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly permissions = signal<PermissionOption[]>([]);
  readonly editingId = signal<string | null>(null);
  readonly editingIsSystem = signal(false);
  readonly error = signal<string | null>(null);

  form: RoleForm = this.emptyForm();

  ngOnInit(): void {
    this.http.get<PermissionOption[]>(`${environment.apiUrl}/roles/permissions`).subscribe({
      next: (permissions) => this.permissions.set(permissions)
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.editingId.set(id);
    this.http.get<RoleRow>(`${environment.apiUrl}/roles/${id}`).subscribe({
      next: (role) => {
        this.editingIsSystem.set(role.isSystem);
        this.form = {
          name: role.name,
          description: role.description ?? '',
          permissionIds: [...(role.permissionIds ?? [])]
        };
        this.loading.set(false);
      },
      error: () => {
        this.messages.errorKey('MSG.NOT_FOUND');
        this.loading.set(false);
      }
    });
  }

  togglePermission(permissionId: string, checked: boolean): void {
    if (checked) {
      if (!this.form.permissionIds.includes(permissionId)) {
        this.form.permissionIds = [...this.form.permissionIds, permissionId];
      }
      return;
    }
    this.form.permissionIds = this.form.permissionIds.filter((id) => id !== permissionId);
  }

  isPermissionSelected(permissionId: string): boolean {
    return this.form.permissionIds.includes(permissionId);
  }

  permissionsByModule(): { module: string; items: PermissionOption[] }[] {
    const groups = new Map<string, PermissionOption[]>();
    for (const permission of this.permissions()) {
      const module = permission.module || 'General';
      const list = groups.get(module) ?? [];
      list.push(permission);
      groups.set(module, list);
    }
    return [...groups.entries()].map(([module, items]) => ({ module, items }));
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    const id = this.editingId();
    const payload = {
      name: this.form.name,
      description: this.form.description,
      permissionIds: this.form.permissionIds
    };

    const request$ = id
      ? this.http.put(`${environment.apiUrl}/roles/${id}`, payload)
      : this.http.post(`${environment.apiUrl}/roles`, payload);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/roles']);
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

  private emptyForm(): RoleForm {
    return {
      name: '',
      description: '',
      permissionIds: []
    };
  }
}
