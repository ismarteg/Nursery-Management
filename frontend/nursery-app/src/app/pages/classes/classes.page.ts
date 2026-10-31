import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

interface ClassRow {
  id: string;
  name: string;
  capacity: number;
  minAgeMonths?: number;
  maxAgeMonths?: number;
  notes?: string;
  isActive: boolean;
  childrenCount: number;
  staffIds: string[];
  staffNames: string[];
}

@Component({
  selector: 'app-classes-page',
  imports: [PageHeaderComponent, TranslatePipe, RouterLink],
  templateUrl: './classes.page.html',
  styleUrl: './classes.page.scss'
})
export class ClassesPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly rows = signal<ClassRow[]>([]);
  readonly error = signal<string | null>(null);
  readonly canWrite = this.auth.hasPermission('classes.write');

  ngOnInit(): void {
    this.reload();
  }

  remove(row: ClassRow): void {
    if (!window.confirm(`Delete class "${row.name}"?`)) {
      return;
    }
    this.http.delete(`${environment.apiUrl}/classes/${row.id}`).subscribe({
      next: () => this.reload(),
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.http.get<ClassRow[]>(`${environment.apiUrl}/classes`).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
