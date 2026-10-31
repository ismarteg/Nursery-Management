import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface ClassOption {
  id: string;
  name: string;
}

interface StaffRow {
  id: string;
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
}

interface StaffForm {
  fullName: string;
  phone: string;
  email: string;
  hireDate: string;
  salaryType: number;
  baseSalary: number;
  hourlyRate: number | null;
  dailyRate: number | null;
  isActive: boolean;
  classIds: string[];
}

@Component({
  selector: 'app-staff-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './staff-form.page.html',
  styleUrl: './staff-form.page.scss'
})
export class StaffFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly classes = signal<ClassOption[]>([]);
  readonly editingId = signal<string | null>(null);

  form: StaffForm = this.emptyForm();

  ngOnInit(): void {
    this.http.get<ClassOption[]>(`${environment.apiUrl}/classes`).subscribe({
      next: (classes) => this.classes.set(classes.map((c) => ({ id: c.id, name: c.name }))),
      error: () => undefined
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.editingId.set(id);
    this.http.get<StaffRow[]>(`${environment.apiUrl}/staff`).subscribe({
      next: (rows) => {
        const row = rows.find((r) => r.id === id);
        if (!row) {
          this.messages.errorKey('MSG.NOT_FOUND');
        } else {
          this.form = {
            fullName: row.fullName,
            phone: row.phone ?? '',
            email: row.email ?? '',
            hireDate: row.hireDate ? row.hireDate.substring(0, 10) : '',
            salaryType: row.salaryType === 'Daily' ? 1 : row.salaryType === 'Hourly' ? 2 : 0,
            baseSalary: row.baseSalary,
            hourlyRate: row.hourlyRate ?? null,
            dailyRate: row.dailyRate ?? null,
            isActive: row.isActive,
            classIds: [...(row.classIds ?? [])]
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

  toggleClass(id: string, checked: boolean): void {
    this.form.classIds = checked
      ? [...new Set([...this.form.classIds, id])]
      : this.form.classIds.filter((x) => x !== id);
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    const payload = {
      fullName: this.form.fullName,
      phone: this.form.phone || null,
      email: this.form.email || null,
      hireDate: this.form.hireDate || null,
      salaryType: this.form.salaryType,
      baseSalary: this.form.baseSalary,
      hourlyRate: this.form.hourlyRate,
      dailyRate: this.form.dailyRate,
      isActive: this.form.isActive,
      classIds: this.form.classIds
    };
    const id = this.editingId();
    const req$ = id
      ? this.http.put(`${environment.apiUrl}/staff/${id}`, payload)
      : this.http.post(`${environment.apiUrl}/staff`, payload);

    req$.subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/staff']);
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

  private emptyForm(): StaffForm {
    return {
      fullName: '',
      phone: '',
      email: '',
      hireDate: '',
      salaryType: 0,
      baseSalary: 0,
      hourlyRate: null,
      dailyRate: null,
      isActive: true,
      classIds: []
    };
  }
}
