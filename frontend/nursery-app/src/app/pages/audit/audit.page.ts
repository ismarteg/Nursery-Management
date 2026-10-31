import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface AuditItem {
  id: string;
  userName?: string;
  action: string;
  entityName: string;
  entityId?: string;
  oldValues?: string;
  newValues?: string;
  ipAddress?: string;
  occurredAt: string;
}

interface AuditResponse {
  total: number;
  items: AuditItem[];
}

@Component({
  selector: 'app-audit-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe],
  templateUrl: './audit.page.html',
  styleUrl: './audit.page.scss'
})
export class AuditPage implements OnInit {
  private readonly http = inject(HttpClient);
  readonly loading = signal(true);
  readonly items = signal<AuditItem[]>([]);
  readonly selected = signal<AuditItem | null>(null);

  userName = '';
  action = '';
  entityName = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    let params = new HttpParams().set('page', '1').set('pageSize', '50');
    if (this.userName.trim()) {
      params = params.set('userName', this.userName.trim());
    }
    if (this.action.trim()) {
      params = params.set('action', this.action.trim());
    }
    if (this.entityName.trim()) {
      params = params.set('entityName', this.entityName.trim());
    }

    this.http.get<AuditResponse>(`${environment.apiUrl}/audit-logs`, { params }).subscribe({
      next: (response) => {
        this.items.set(response.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
