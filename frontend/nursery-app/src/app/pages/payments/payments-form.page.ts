import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface ChildOption {
  id: string;
  code: string;
  fullName: string;
  pricePlanId?: string;
}

interface PlanOption {
  id: string;
  name: string;
  amount: number;
}

interface PaymentForm {
  childId: string;
  pricePlanId: string;
  amountDue: number;
  amountPaid: number;
  dueDate: string;
  method: number;
  notes: string;
}

@Component({
  selector: 'app-payments-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './payments-form.page.html',
  styleUrl: './payments-form.page.scss'
})
export class PaymentsFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly children = signal<ChildOption[]>([]);
  readonly plans = signal<PlanOption[]>([]);

  form: PaymentForm = this.emptyForm();

  ngOnInit(): void {
    this.http.get<ChildOption[]>(`${environment.apiUrl}/children`).subscribe({
      next: (children) =>
        this.children.set(
          children.map((c) => ({
            id: c.id,
            code: c.code,
            fullName: c.fullName,
            pricePlanId: c.pricePlanId
          }))
        )
    });
    this.http.get<PlanOption[]>(`${environment.apiUrl}/price-plans?activeOnly=true`).subscribe({
      next: (plans) => this.plans.set(plans.map((p) => ({ id: p.id, name: p.name, amount: p.amount })))
    });
  }

  onChildChange(): void {
    const child = this.children().find((c) => c.id === this.form.childId);
    if (!child?.pricePlanId) {
      return;
    }
    this.form.pricePlanId = child.pricePlanId;
    this.onPlanChange();
  }

  onPlanChange(): void {
    const plan = this.plans().find((p) => p.id === this.form.pricePlanId);
    if (plan) {
      this.form.amountDue = plan.amount;
    }
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    this.http
      .post(`${environment.apiUrl}/payments`, {
        childId: this.form.childId,
        pricePlanId: this.form.pricePlanId || null,
        amountDue: this.form.amountDue,
        amountPaid: this.form.amountPaid,
        dueDate: this.form.dueDate,
        method: this.form.amountPaid > 0 ? this.form.method : null,
        notes: this.form.notes || null
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          void this.router.navigate(['/payments']);
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

  private emptyForm(): PaymentForm {
    const due = new Date();
    due.setDate(due.getDate() + 30);
    return {
      childId: '',
      pricePlanId: '',
      amountDue: 0,
      amountPaid: 0,
      dueDate: due.toISOString().substring(0, 10),
      method: 0,
      notes: ''
    };
  }
}
