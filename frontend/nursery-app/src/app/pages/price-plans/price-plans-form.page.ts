import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface PlanRow {
  id: string;
  name: string;
  description?: string;
  amount: number;
  billingCycle: string;
  daysCount: number;
  isActive: boolean;
}

interface PlanForm {
  name: string;
  description: string;
  amount: number;
  billingCycle: number;
  daysCount: number;
  isActive: boolean;
}

@Component({
  selector: 'app-price-plans-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './price-plans-form.page.html',
  styleUrl: './price-plans-form.page.scss'
})
export class PricePlansFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);

  form: PlanForm = this.emptyForm();

  readonly cycles = [
    { value: 0, labelKey: 'PLANS.CYCLE_MONTHLY', defaultDays: 30 },
    { value: 1, labelKey: 'PLANS.CYCLE_TERM', defaultDays: 90 },
    { value: 2, labelKey: 'PLANS.CYCLE_YEARLY', defaultDays: 365 },
    { value: 3, labelKey: 'PLANS.CYCLE_DAILY', defaultDays: 1 }
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.editingId.set(id);
    this.http.get<PlanRow[]>(`${environment.apiUrl}/price-plans`).subscribe({
      next: (rows) => {
        const row = rows.find((r) => r.id === id);
        if (!row) {
          this.messages.errorKey('MSG.NOT_FOUND');
        } else {
          this.form = {
            name: row.name,
            description: row.description ?? '',
            amount: row.amount,
            billingCycle: this.cycleToNumber(row.billingCycle),
            daysCount: row.daysCount > 0 ? row.daysCount : this.defaultDaysFor(row.billingCycle),
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

  onCycleChange(): void {
    const cycle = this.cycles.find((c) => c.value === this.form.billingCycle);
    if (cycle) {
      this.form.daysCount = cycle.defaultDays;
    }
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    const payload = {
      ...this.form,
      daysCount: Math.max(1, Number(this.form.daysCount) || 1)
    };
    const id = this.editingId();
    const req$ = id
      ? this.http.put(`${environment.apiUrl}/price-plans/${id}`, payload)
      : this.http.post(`${environment.apiUrl}/price-plans`, payload);

    req$.subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/price-plans']);
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

  private cycleToNumber(cycle: string): number {
    switch (cycle) {
      case 'Term':
        return 1;
      case 'Yearly':
        return 2;
      case 'Daily':
        return 3;
      default:
        return 0;
    }
  }

  private defaultDaysFor(cycle: string): number {
    return this.cycles.find((c) => c.value === this.cycleToNumber(cycle))?.defaultDays ?? 30;
  }

  private emptyForm(): PlanForm {
    return {
      name: '',
      description: '',
      amount: 0,
      billingCycle: 0,
      daysCount: 30,
      isActive: true
    };
  }
}
