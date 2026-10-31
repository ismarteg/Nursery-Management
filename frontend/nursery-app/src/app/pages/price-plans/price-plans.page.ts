import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

interface PlanRow {
  id: string;
  name: string;
  description?: string;
  amount: number;
  billingCycle: string;
  daysCount: number;
  isActive: boolean;
  childrenCount: number;
}

@Component({
  selector: 'app-price-plans-page',
  imports: [PageHeaderComponent, TranslatePipe, RouterLink],
  templateUrl: './price-plans.page.html',
  styleUrl: './price-plans.page.scss'
})
export class PricePlansPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly rows = signal<PlanRow[]>([]);
  readonly error = signal<string | null>(null);
  readonly canWrite = this.auth.hasPermission('priceplans.write');

  ngOnInit(): void {
    this.reload();
  }

  remove(row: PlanRow): void {
    if (!window.confirm(`Delete/deactivate plan "${row.name}"?`)) {
      return;
    }
    this.http.delete(`${environment.apiUrl}/price-plans/${row.id}`).subscribe({
      next: () => this.reload(),
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.http.get<PlanRow[]>(`${environment.apiUrl}/price-plans`).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
