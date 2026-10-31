import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

interface SalaryRow {
  id: string;
  staffCode: string;
  staffName: string;
  year: number;
  month: number;
  workedDays: number;
  workedHours: number;
  amount: number;
  salaryType: string;
  status: string;
  calculatedAt: string;
  paidAt?: string;
}

@Component({
  selector: 'app-salaries-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe],
  templateUrl: './salaries.page.html',
  styleUrl: './salaries.page.scss'
})
export class SalariesPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly calculating = signal(false);
  readonly rows = signal<SalaryRow[]>([]);
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly canCalculate = this.auth.hasPermission('salaries.calculate');

  year = new Date().getFullYear();
  month = new Date().getMonth() + 1;

  ngOnInit(): void {
    this.reload();
  }

  calculate(): void {
    this.calculating.set(true);
    this.error.set(null);
    this.message.set(null);
    this.http
      .post<{ created: number; updated: number }>(`${environment.apiUrl}/salaries/calculate`, {
        year: this.year,
        month: this.month
      })
      .subscribe({
        next: (result) => {
          this.calculating.set(false);
          this.messages.success(`created=${result.created}, updated=${result.updated}`);
          this.reload();
        },
        error: (err) => {
          this.calculating.set(false);
          this.messages.fromHttpError(err);
        }
      });
  }

  markPaid(row: SalaryRow): void {
    this.http.post(`${environment.apiUrl}/salaries/${row.id}/mark-paid`, {}).subscribe({
      next: () => this.reload(),
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  reload(): void {
    this.loading.set(true);
    const params = new HttpParams().set('year', this.year).set('month', this.month);
    this.http.get<SalaryRow[]>(`${environment.apiUrl}/salaries`, { params }).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
