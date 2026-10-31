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

interface PaymentRow {
  id: string;
  childId: string;
  childCode: string;
  childName: string;
  planName?: string;
  amountDue: number;
  amountPaid: number;
  balance: number;
  dueDate: string;
  paidAt?: string;
  status: string;
  method?: string;
  notes?: string;
}

@Component({
  selector: 'app-payments-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe, RouterLink],
  templateUrl: './payments.page.html',
  styleUrl: './payments.page.scss'
})
export class PaymentsPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly rows = signal<PaymentRow[]>([]);
  readonly receivables = signal<{ count: number; totalBalance: number }>({ count: 0, totalBalance: 0 });
  readonly error = signal<string | null>(null);
  readonly canWrite = this.auth.hasPermission('payments.write');

  statusFilter = '';

  ngOnInit(): void {
    this.reload();
  }

  cancel(row: PaymentRow): void {
    if (!window.confirm(`Cancel payment for ${row.childName}?`)) {
      return;
    }
    this.http.delete(`${environment.apiUrl}/payments/${row.id}`).subscribe({
      next: () => this.reload(),
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  reload(): void {
    this.loading.set(true);
    let params = new HttpParams();
    if (this.statusFilter) {
      params = params.set('status', this.statusFilter);
    }

    this.http.get<PaymentRow[]>(`${environment.apiUrl}/payments`, { params }).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.http
      .get<{ count: number; totalBalance: number }>(`${environment.apiUrl}/payments/receivables`)
      .subscribe({
        next: (summary) => this.receivables.set(summary)
      });
  }

}


