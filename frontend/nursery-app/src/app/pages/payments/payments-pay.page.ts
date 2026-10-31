import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface PaymentRow {
  id: string;
  childCode: string;
  childName: string;
  balance: number;
}

@Component({
  selector: 'app-payments-pay-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './payments-pay.page.html',
  styleUrl: './payments-pay.page.scss'
})
export class PaymentsPayPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly payingId = signal<string | null>(null);
  readonly childLabel = signal<string | null>(null);

  payAmount = 0;
  payMethod = 0;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.messages.errorKey('MSG.NOT_FOUND');
      this.loading.set(false);
      return;
    }

    this.http.get<PaymentRow[]>(`${environment.apiUrl}/payments`).subscribe({
      next: (rows) => {
        const row = rows.find((r) => r.id === id);
        if (!row) {
          this.messages.errorKey('MSG.NOT_FOUND');
        } else {
          this.payingId.set(row.id);
          this.payAmount = row.balance;
          this.childLabel.set(`${row.childName} (${row.childCode})`);
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
    const id = this.payingId();
    if (!id) {
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.http
      .post(`${environment.apiUrl}/payments/${id}/pay`, {
        amount: this.payAmount,
        method: this.payMethod
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
}
