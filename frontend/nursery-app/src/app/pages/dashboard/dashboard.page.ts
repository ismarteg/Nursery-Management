import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

interface DashboardSummary {
  generatedAt: string;
  year: number;
  month: number;
  children?: { activeCount: number } | null;
  present?: { count: number } | null;
  receivables?: {
    openCount: number;
    totalBalance: number;
    overdueCount: number;
    overdueBalance: number;
  } | null;
  expenses?: { year: number; month: number; count: number; totalAmount: number } | null;
  payroll?: {
    year: number;
    month: number;
    count: number;
    totalAmount: number;
    paidAmount: number;
    unpaidAmount: number;
  } | null;
  staff?: { activeCount: number } | null;
  classes?: { activeCount: number } | null;
}

@Component({
  selector: 'app-dashboard-page',
  imports: [PageHeaderComponent, TranslatePipe, RouterLink, DatePipe],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss'
})
export class DashboardPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly summary = signal<DashboardSummary | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<DashboardSummary>(`${environment.apiUrl}/dashboard/summary`).subscribe({
      next: (data) => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.messages.fromHttpError(err, 'DASHBOARD.LOAD_ERROR');
        this.loading.set(false);
      }
    });
  }
}
