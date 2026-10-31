import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

interface AttendanceRow {
  id: string;
  staffCode: string;
  staffName: string;
  workDate: string;
  clockIn: string;
  clockOut: string;
  workedHours: number;
}

interface UploadResult {
  fileName: string;
  totalRows: number;
  successRows: number;
  failedRows: number;
  errors: string[];
}

@Component({
  selector: 'app-attendance-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe],
  templateUrl: './attendance.page.html',
  styleUrl: './attendance.page.scss'
})
export class AttendancePage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly uploading = signal(false);
  readonly rows = signal<AttendanceRow[]>([]);
  readonly uploadResult = signal<UploadResult | null>(null);
  readonly error = signal<string | null>(null);
  readonly canUpload = this.auth.hasPermission('salaries.calculate');

  year = new Date().getFullYear();
  month = new Date().getMonth() + 1;
  selectedFile: File | null = null;

  ngOnInit(): void {
    this.reload();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  upload(): void {
    if (!this.selectedFile) {
      return;
    }

    this.uploading.set(true);
    this.error.set(null);
    this.uploadResult.set(null);

    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.http.post<UploadResult>(`${environment.apiUrl}/attendance/upload`, formData).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.uploadResult.set(result);
        this.selectedFile = null;
        this.reload();
      },
      error: (err) => {
        this.uploading.set(false);
        this.messages.fromHttpError(err);
      }
    });
  }

  reload(): void {
    this.loading.set(true);
    const params = new HttpParams().set('year', this.year).set('month', this.month);
    this.http.get<AttendanceRow[]>(`${environment.apiUrl}/attendance`, { params }).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  downloadSample(): void {
    const csv = [
      'StaffCode,Date,ClockIn,ClockOut',
      'STF-000001,2026-10-01,08:00,16:00',
      'STF-000001,2026-10-02,08:15,16:10'
    ].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'attendance-sample.csv';
    a.click();
    URL.revokeObjectURL(url);
  }
}
