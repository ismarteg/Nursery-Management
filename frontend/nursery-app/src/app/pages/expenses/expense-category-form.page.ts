import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface CategoryRow {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

interface CategoryForm {
  name: string;
  description: string;
  isActive: boolean;
}

@Component({
  selector: 'app-expense-category-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './expense-category-form.page.html',
  styleUrl: './expense-category-form.page.scss'
})
export class ExpenseCategoryFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);

  categoryForm: CategoryForm = { name: '', description: '', isActive: true };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.editingId.set(id);
    this.http.get<CategoryRow[]>(`${environment.apiUrl}/expense-categories`).subscribe({
      next: (rows) => {
        const row = rows.find((r) => r.id === id);
        if (!row) {
          this.messages.errorKey('MSG.NOT_FOUND');
        } else {
          this.categoryForm = {
            name: row.name,
            description: row.description ?? '',
            isActive: row.isActive
          };
        }
        this.loading.set(false);
      },
      error: () => {
        this.messages.errorKey('MSG.NOT_FOUND');
        this.loading.set(false);
      }
    });
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    const id = this.editingId();
    const req$ = id
      ? this.http.put(`${environment.apiUrl}/expense-categories/${id}`, this.categoryForm)
      : this.http.post(`${environment.apiUrl}/expense-categories`, this.categoryForm);

    req$.subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/expenses']);
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
