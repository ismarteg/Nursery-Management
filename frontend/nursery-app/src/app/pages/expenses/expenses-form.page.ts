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
  isActive: boolean;
}

interface ExpenseRow {
  id: string;
  categoryId: string;
  title: string;
  amount: number;
  expenseDate: string;
  paidTo?: string;
  notes?: string;
}

interface ExpenseForm {
  categoryId: string;
  title: string;
  amount: number;
  expenseDate: string;
  paidTo: string;
  notes: string;
}

@Component({
  selector: 'app-expenses-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './expenses-form.page.html',
  styleUrl: './expenses-form.page.scss'
})
export class ExpensesFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly categories = signal<CategoryRow[]>([]);
  readonly editingId = signal<string | null>(null);

  form: ExpenseForm = this.emptyForm();

  ngOnInit(): void {
    this.http.get<CategoryRow[]>(`${environment.apiUrl}/expense-categories`).subscribe({
      next: (categories) => this.categories.set(categories)
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.editingId.set(id);
    this.http
      .get<{ items: ExpenseRow[] }>(`${environment.apiUrl}/expenses`)
      .subscribe({
        next: (response) => {
          const row = response.items.find((r) => r.id === id);
          if (!row) {
            this.messages.errorKey('MSG.NOT_FOUND');
          } else {
            this.form = {
              categoryId: row.categoryId,
              title: row.title,
              amount: row.amount,
              expenseDate: row.expenseDate.substring(0, 10),
              paidTo: row.paidTo ?? '',
              notes: row.notes ?? ''
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
    const payload = {
      categoryId: this.form.categoryId,
      title: this.form.title,
      amount: this.form.amount,
      expenseDate: this.form.expenseDate,
      paidTo: this.form.paidTo || null,
      notes: this.form.notes || null
    };
    const id = this.editingId();
    const req$ = id
      ? this.http.put(`${environment.apiUrl}/expenses/${id}`, payload)
      : this.http.post(`${environment.apiUrl}/expenses`, payload);

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

  private emptyForm(): ExpenseForm {
    return {
      categoryId: '',
      title: '',
      amount: 0,
      expenseDate: new Date().toISOString().substring(0, 10),
      paidTo: '',
      notes: ''
    };
  }
}
