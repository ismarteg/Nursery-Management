import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface StaffOption {
  id: string;
  fullName: string;
}

interface ClassRow {
  id: string;
  name: string;
  capacity: number;
  minAgeMonths?: number;
  maxAgeMonths?: number;
  notes?: string;
  isActive: boolean;
  staffIds: string[];
}

interface ClassForm {
  name: string;
  capacity: number;
  minAgeMonths: number | null;
  maxAgeMonths: number | null;
  notes: string;
  isActive: boolean;
  staffIds: string[];
}

@Component({
  selector: 'app-classes-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './classes-form.page.html',
  styleUrl: './classes-form.page.scss'
})
export class ClassesFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly staff = signal<StaffOption[]>([]);
  readonly editingId = signal<string | null>(null);

  form: ClassForm = this.emptyForm();

  ngOnInit(): void {
    this.http.get<StaffOption[]>(`${environment.apiUrl}/staff`).subscribe({
      next: (staff) => this.staff.set(staff.map((s) => ({ id: s.id, fullName: s.fullName }))),
      error: () => undefined
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.editingId.set(id);
    this.http.get<ClassRow[]>(`${environment.apiUrl}/classes`).subscribe({
      next: (rows) => {
        const row = rows.find((r) => r.id === id);
        if (!row) {
          this.messages.errorKey('MSG.NOT_FOUND');
        } else {
          this.form = {
            name: row.name,
            capacity: row.capacity,
            minAgeMonths: row.minAgeMonths ?? null,
            maxAgeMonths: row.maxAgeMonths ?? null,
            notes: row.notes ?? '',
            isActive: row.isActive,
            staffIds: [...(row.staffIds ?? [])]
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

  toggleStaff(id: string, checked: boolean): void {
    this.form.staffIds = checked
      ? [...new Set([...this.form.staffIds, id])]
      : this.form.staffIds.filter((x) => x !== id);
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    const payload = {
      name: this.form.name,
      capacity: this.form.capacity,
      minAgeMonths: this.form.minAgeMonths,
      maxAgeMonths: this.form.maxAgeMonths,
      notes: this.form.notes,
      isActive: this.form.isActive,
      staffIds: this.form.staffIds
    };
    const id = this.editingId();
    const req$ = id
      ? this.http.put(`${environment.apiUrl}/classes/${id}`, payload)
      : this.http.post(`${environment.apiUrl}/classes`, payload);

    req$.subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/classes']);
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

  private emptyForm(): ClassForm {
    return {
      name: '',
      capacity: 20,
      minAgeMonths: null,
      maxAgeMonths: null,
      notes: '',
      isActive: true,
      staffIds: []
    };
  }
}
