import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface ClassOption {
  id: string;
  name: string;
}

interface EntryItem {
  id: string;
  childCode: string;
  childName: string;
  className?: string;
  type: string;
  method: string;
  occurredAt: string;
  notes?: string;
}

@Component({
  selector: 'app-entry-history-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe, RouterLink],
  templateUrl: './entry-history.page.html',
  styleUrl: './entry-history.page.scss'
})
export class EntryHistoryPage implements OnInit {
  private readonly http = inject(HttpClient);

  readonly loading = signal(true);
  readonly items = signal<EntryItem[]>([]);
  readonly total = signal(0);
  readonly classes = signal<ClassOption[]>([]);

  classId = '';
  type = '';
  from = '';
  to = '';

  ngOnInit(): void {
    this.http.get<ClassOption[]>(`${environment.apiUrl}/classes`).subscribe({
      next: (classes) => this.classes.set(classes.map((c) => ({ id: c.id, name: c.name }))),
      error: () => undefined
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    let params = new HttpParams().set('page', '1').set('pageSize', '100');
    if (this.classId) {
      params = params.set('classId', this.classId);
    }
    if (this.type) {
      params = params.set('type', this.type);
    }
    if (this.from) {
      params = params.set('from', new Date(this.from).toISOString());
    }
    if (this.to) {
      params = params.set('to', new Date(`${this.to}T23:59:59`).toISOString());
    }

    this.http
      .get<{ total: number; items: EntryItem[] }>(`${environment.apiUrl}/entry`, { params })
      .subscribe({
        next: (response) => {
          this.total.set(response.total);
          this.items.set(response.items);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }
}
