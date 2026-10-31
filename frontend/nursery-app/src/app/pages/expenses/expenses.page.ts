import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

interface CategoryRow {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  expensesCount: number;
}

interface ExpenseRow {
  id: string;
  categoryId: string;
  categoryName: string;
  title: string;
  amount: number;
  expenseDate: string;
  paidTo?: string;
  notes?: string;
}

@Component({
  selector: 'app-expenses-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe, RouterLink],
  templateUrl: './expenses.page.html',
  styleUrl: './expenses.page.scss'
})
export class ExpensesPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly rows = signal<ExpenseRow[]>([]);
  readonly categories = signal<CategoryRow[]>([]);
  readonly totalAmount = signal(0);
  readonly monthlyTotal = signal(0);
  readonly error = signal<string | null>(null);
  readonly canWrite = this.auth.hasPermission('expenses.write');

  categoryId = '';
  from = '';
  to = '';

  ngOnInit(): void {
    this.reloadCategories();
    this.reload();
    this.reloadMonthlySummary();
  }

  removeExpense(row: ExpenseRow): void {
    if (!window.confirm(`Delete expense "${row.title}"?`)) {
      return;
    }
    this.http.delete(`${environment.apiUrl}/expenses/${row.id}`).subscribe({
      next: () => {
        this.reload();
        this.reloadMonthlySummary();
        this.reloadCategories();
      },
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  removeCategory(row: CategoryRow): void {
    if (!window.confirm(`Delete/deactivate category "${row.name}"?`)) {
      return;
    }
    this.http.delete(`${environment.apiUrl}/expense-categories/${row.id}`).subscribe({
      next: () => this.reloadCategories(),
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  reload(): void {
    this.loading.set(true);
    let params = new HttpParams();
    if (this.categoryId) {
      params = params.set('categoryId', this.categoryId);
    }
    if (this.from) {
      params = params.set('from', this.from);
    }
    if (this.to) {
      params = params.set('to', this.to);
    }

    this.http
      .get<{ totalAmount: number; items: ExpenseRow[] }>(`${environment.apiUrl}/expenses`, { params })
      .subscribe({
        next: (response) => {
          this.totalAmount.set(response.totalAmount);
          this.rows.set(response.items);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  private reloadCategories(): void {
    this.http.get<CategoryRow[]>(`${environment.apiUrl}/expense-categories`).subscribe({
      next: (categories) => this.categories.set(categories)
    });
  }

  private reloadMonthlySummary(): void {
    this.http.get<{ totalAmount: number }>(`${environment.apiUrl}/expenses/summary`).subscribe({
      next: (summary) => this.monthlyTotal.set(summary.totalAmount)
    });
  }
}
