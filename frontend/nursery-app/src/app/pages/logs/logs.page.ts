import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface LogItem {
  id: string;
  logType: string;
  module: string;
  action: string;
  message: string;
  details?: string;
  succeeded: boolean;
  statusCode?: number;
  userName?: string;
  ipAddress?: string;
  occurredAt: string;
}

interface LogsResponse {
  total: number;
  items: LogItem[];
}

@Component({
  selector: 'app-logs-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe],
  templateUrl: './logs.page.html',
  styleUrl: './logs.page.scss'
})
export class LogsPage implements OnInit {
  private readonly http = inject(HttpClient);

  readonly loading = signal(true);
  readonly items = signal<LogItem[]>([]);
  readonly selected = signal<LogItem | null>(null);
  readonly total = signal(0);

  logType = '';
  module = '';
  userName = '';

  readonly logTypes = ['Info', 'Success', 'Warning', 'Error'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    let params = new HttpParams().set('page', '1').set('pageSize', '100');
    if (this.logType) {
      params = params.set('logType', this.logType);
    }
    if (this.module.trim()) {
      params = params.set('module', this.module.trim());
    }
    if (this.userName.trim()) {
      params = params.set('userName', this.userName.trim());
    }

    this.http.get<LogsResponse>(`${environment.apiUrl}/logs`, { params }).subscribe({
      next: (response) => {
        this.items.set(response.items);
        this.total.set(response.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
