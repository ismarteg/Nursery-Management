import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

interface StaffRow {
  id: string;
  code: string;
  fullName: string;
  phone?: string;
  email?: string;
  hireDate?: string;
  salaryType: string;
  baseSalary: number;
  hourlyRate?: number;
  dailyRate?: number;
  isActive: boolean;
  classIds: string[];
  classNames: string[];
}

@Component({
  selector: 'app-staff-page',
  imports: [PageHeaderComponent, TranslatePipe, RouterLink],
  templateUrl: './staff.page.html',
  styleUrl: './staff.page.scss'
})
export class StaffPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly rows = signal<StaffRow[]>([]);
  readonly error = signal<string | null>(null);
  readonly canWrite = this.auth.hasPermission('staff.write');

  ngOnInit(): void {
    this.reload();
  }

  remove(row: StaffRow): void {
    if (!window.confirm(`Delete staff "${row.fullName}"?`)) {
      return;
    }
    this.http.delete(`${environment.apiUrl}/staff/${row.id}`).subscribe({
      next: () => this.reload(),
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.http.get<StaffRow[]>(`${environment.apiUrl}/staff`).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
