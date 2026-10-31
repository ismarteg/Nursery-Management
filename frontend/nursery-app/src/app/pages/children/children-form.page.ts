import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

interface ClassOption {
  id: string;
  name: string;
}

interface PlanOption {
  id: string;
  name: string;
  amount: number;
}

interface ParentForm {
  fullName: string;
  phone: string;
  email: string;
  nationalId: string;
}

interface ChildDetail {
  id: string;
  code: string;
  fullName: string;
  birthDate?: string;
  gender?: string;
  photoPath?: string;
  father?: ParentForm & { id?: string; photoPath?: string };
  mother?: ParentForm & { id?: string; photoPath?: string };
  guardian?: { name: string; phone?: string; email?: string; relation?: string };
  classId?: string;
  pricePlanId?: string;
  notes?: string;
  isActive: boolean;
  qrCode: string;
  barcode: string;
  entryQrCode?: string;
  entryBarcode?: string;
}

const RELATIONS = ['Brother', 'Sister', 'Grandfather', 'Grandmother', 'Uncle', 'Aunt', 'Other'];

@Component({
  selector: 'app-children-form-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './children-form.page.html',
  styleUrl: './children-form.page.scss'
})
export class ChildrenFormPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly relations = RELATIONS;
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly classes = signal<ClassOption[]>([]);
  readonly plans = signal<PlanOption[]>([]);
  readonly childId = signal<string | null>(null);
  readonly identityCode = signal<string | null>(null);
  readonly entryCode = signal<string | null>(null);
  readonly childPhotoUrl = signal<string | null>(null);
  readonly fatherPhotoUrl = signal<string | null>(null);
  readonly motherPhotoUrl = signal<string | null>(null);

  enrollmentEnabled = false;
  guardianEnabled = false;
  manualEntryCode = '';
  form = this.emptyForm();

  private pendingChildPhoto: File | null = null;
  private pendingFatherPhoto: File | null = null;
  private pendingMotherPhoto: File | null = null;

  ngOnInit(): void {
    this.http.get<ClassOption[]>(`${environment.apiUrl}/classes`).subscribe({
      next: (rows) => this.classes.set(rows.map((c) => ({ id: c.id, name: c.name })))
    });
    this.http.get<PlanOption[]>(`${environment.apiUrl}/price-plans?activeOnly=true`).subscribe({
      next: (rows) => this.plans.set(rows.map((p) => ({ id: p.id, name: p.name, amount: p.amount })))
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.childId.set(id);
    this.http.get<ChildDetail>(`${environment.apiUrl}/children/${id}`).subscribe({
      next: (child) => {
        this.form = {
          fullName: child.fullName,
          birthDate: child.birthDate ? child.birthDate.substring(0, 10) : '',
          gender: child.gender ?? '',
          father: {
            fullName: child.father?.fullName ?? '',
            phone: child.father?.phone ?? '',
            email: child.father?.email ?? '',
            nationalId: child.father?.nationalId ?? ''
          },
          mother: {
            fullName: child.mother?.fullName ?? '',
            phone: child.mother?.phone ?? '',
            email: child.mother?.email ?? '',
            nationalId: child.mother?.nationalId ?? ''
          },
          guardianName: child.guardian?.name ?? '',
          guardianPhone: child.guardian?.phone ?? '',
          guardianEmail: child.guardian?.email ?? '',
          guardianRelation: child.guardian?.relation ?? '',
          classId: child.classId ?? '',
          pricePlanId: child.pricePlanId ?? '',
          notes: child.notes ?? '',
          isActive: child.isActive
        };
        this.enrollmentEnabled = !!(child.classId || child.pricePlanId || child.notes);
        this.guardianEnabled = !!(
          child.guardian?.name ||
          child.guardian?.phone ||
          child.guardian?.email ||
          child.guardian?.relation
        );
        this.identityCode.set(child.qrCode);
        this.entryCode.set(child.entryQrCode ?? null);
        this.loadPhoto(child.photoPath, this.childPhotoUrl);
        this.loadPhoto(child.father?.photoPath, this.fatherPhotoUrl);
        this.loadPhoto(child.mother?.photoPath, this.motherPhotoUrl);
        this.loading.set(false);
      },
      error: () => {
        this.messages.errorKey('MSG.NOT_FOUND');
        this.loading.set(false);
      }
    });
  }

  onEnrollmentToggle(enabled: boolean): void {
    if (!enabled) {
      this.form.classId = '';
      this.form.pricePlanId = '';
      this.form.notes = '';
    }
  }

  onGuardianToggle(enabled: boolean): void {
    if (!enabled) {
      this.form.guardianName = '';
      this.form.guardianPhone = '';
      this.form.guardianEmail = '';
      this.form.guardianRelation = '';
    }
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    const payload = {
      fullName: this.form.fullName,
      birthDate: this.form.birthDate || null,
      gender: this.form.gender || null,
      father: {
        fullName: this.form.father.fullName,
        phone: this.form.father.phone,
        email: this.form.father.email || null,
        nationalId: this.form.father.nationalId || null
      },
      mother: {
        fullName: this.form.mother.fullName,
        phone: this.form.mother.phone,
        email: this.form.mother.email || null,
        nationalId: this.form.mother.nationalId || null
      },
      guardian: this.guardianEnabled
        ? {
            name: this.form.guardianName || null,
            phone: this.form.guardianPhone || null,
            email: this.form.guardianEmail || null,
            relation: this.form.guardianRelation || null
          }
        : null,
      classId: this.enrollmentEnabled ? this.form.classId || null : null,
      pricePlanId: this.enrollmentEnabled ? this.form.pricePlanId || null : null,
      notes: this.enrollmentEnabled ? this.form.notes || null : null,
      isActive: this.form.isActive
    };

    const id = this.childId();
    const req$ = id
      ? this.http.put(`${environment.apiUrl}/children/${id}`, payload)
      : this.http.post<{ id: string }>(`${environment.apiUrl}/children`, payload);

    req$.subscribe({
      next: (res) => {
        if (!id) {
          const createdId = (res as { id: string }).id;
          this.childId.set(createdId);
          this.uploadPendingPhotos(createdId, () => {
            this.saving.set(false);
            void this.router.navigate(['/children', createdId, 'edit']);
          });
          return;
        }

        this.saving.set(false);
        void this.router.navigate(['/children']);
      },
      error: (err) => {
        this.saving.set(false);
        this.handleActionError(err);
      }
    });
  }

  onChildPhotoSelected(event: Event): void {
    this.onPhotoSelected(event, 'child');
  }

  onFatherPhotoSelected(event: Event): void {
    this.onPhotoSelected(event, 'father');
  }

  onMotherPhotoSelected(event: Event): void {
    this.onPhotoSelected(event, 'mother');
  }

  generateEntryCode(): void {
    const id = this.childId();
    if (!id) {
      return;
    }
    this.http.post<{ entryQrCode: string }>(`${environment.apiUrl}/children/${id}/entry-code/generate`, {}).subscribe({
      next: (res) => this.entryCode.set(res.entryQrCode),
      error: (err) => this.handleActionError(err)
    });
  }

  setEntryCode(): void {
    const id = this.childId();
    if (!id || !this.manualEntryCode.trim()) {
      return;
    }
    this.http
      .put<{ entryQrCode: string }>(`${environment.apiUrl}/children/${id}/entry-code`, {
        code: this.manualEntryCode.trim()
      })
      .subscribe({
        next: (res) => {
          this.entryCode.set(res.entryQrCode);
          this.manualEntryCode = '';
        },
        error: (err) => this.handleActionError(err)
      });
  }

  clearEntryCode(): void {
    const id = this.childId();
    if (!id) {
      return;
    }
    this.http.delete(`${environment.apiUrl}/children/${id}/entry-code`).subscribe({
      next: () => this.entryCode.set(null),
      error: (err) => this.handleActionError(err)
    });
  }

  private onPhotoSelected(event: Event, kind: 'child' | 'father' | 'mother'): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    const preview = URL.createObjectURL(file);
    if (kind === 'child') {
      this.childPhotoUrl.set(preview);
      this.pendingChildPhoto = file;
    } else if (kind === 'father') {
      this.fatherPhotoUrl.set(preview);
      this.pendingFatherPhoto = file;
    } else {
      this.motherPhotoUrl.set(preview);
      this.pendingMotherPhoto = file;
    }

    const id = this.childId();
    if (id) {
      this.uploadFile(id, kind, file);
    }
  }

  private uploadPendingPhotos(id: string, done: () => void): void {
    const jobs: Array<{ kind: 'child' | 'father' | 'mother'; file: File }> = [];
    if (this.pendingChildPhoto) {
      jobs.push({ kind: 'child', file: this.pendingChildPhoto });
    }
    if (this.pendingFatherPhoto) {
      jobs.push({ kind: 'father', file: this.pendingFatherPhoto });
    }
    if (this.pendingMotherPhoto) {
      jobs.push({ kind: 'mother', file: this.pendingMotherPhoto });
    }

    if (jobs.length === 0) {
      done();
      return;
    }

    let remaining = jobs.length;
    for (const job of jobs) {
      this.uploadFile(id, job.kind, job.file, () => {
        remaining -= 1;
        if (remaining === 0) {
          done();
        }
      });
    }
  }

  private uploadFile(
    id: string,
    kind: 'child' | 'father' | 'mother',
    file: File,
    done?: () => void
  ): void {
    const formData = new FormData();
    formData.append('file', file);
    const url =
      kind === 'child'
        ? `${environment.apiUrl}/children/${id}/photo`
        : `${environment.apiUrl}/children/${id}/parents/${kind}/photo`;

    this.http.post(url, formData).subscribe({
      next: () => {
        if (kind === 'child') {
          this.pendingChildPhoto = null;
        } else if (kind === 'father') {
          this.pendingFatherPhoto = null;
        } else {
          this.pendingMotherPhoto = null;
        }
        done?.();
      },
      error: (err) => {
        this.handleActionError(err);
        done?.();
      }
    });
  }

  private loadPhoto(path: string | undefined, target: { set: (v: string | null) => void }): void {
    if (!path) {
      target.set(null);
      return;
    }
    this.http.get(`${environment.apiUrl}/files/${path}`, { responseType: 'blob' }).subscribe({
      next: (blob) => target.set(URL.createObjectURL(blob)),
      error: () => target.set(null)
    });
  }

  private emptyForm() {
    return {
      fullName: '',
      birthDate: '',
      gender: '',
      father: { fullName: '', phone: '', email: '', nationalId: '' } as ParentForm,
      mother: { fullName: '', phone: '', email: '', nationalId: '' } as ParentForm,
      guardianName: '',
      guardianPhone: '',
      guardianEmail: '',
      guardianRelation: '',
      classId: '',
      pricePlanId: '',
      notes: '',
      isActive: true
    };
  }

  private handleActionError(err: unknown): void {
    if (!this.messages.fromHttpError(err)) {
      this.error.set(this.messages.validationText(err));
    } else {
      this.error.set(null);
    }
  }
}
