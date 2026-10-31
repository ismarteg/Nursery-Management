import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';

interface ChildInfo {
  id: string;
  code: string;
  fullName: string;
  className?: string;
}

interface EntryHistoryItem {
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
  selector: 'app-children-entry-history-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe, RouterLink],
  templateUrl: './children-entry-history.page.html',
  styleUrl: './children-entry-history.page.scss'
})
export class ChildrenEntryHistoryPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly childLoading = signal(true);
  readonly child = signal<ChildInfo | null>(null);
  readonly items = signal<EntryHistoryItem[]>([]);
  readonly total = signal(0);

  childId = '';
  type = '';
  from = '';
  to = '';

  ngOnInit(): void {
    this.childId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.childId) {
      this.childLoading.set(false);
      this.loading.set(false);
      return;
    }

    this.http.get<ChildInfo>(`${environment.apiUrl}/children/${this.childId}`).subscribe({
      next: (child) => {
        this.child.set(child);
        this.childLoading.set(false);
      },
      error: (err) => {
        this.childLoading.set(false);
        this.messages.fromHttpError(err, 'CHILDREN.HISTORY_LOAD_FAILED');
      }
    });

    this.load();
  }

  load(): void {
    if (!this.childId) {
      return;
    }

    this.loading.set(true);
    let params = new HttpParams()
      .set('childId', this.childId)
      .set('page', '1')
      .set('pageSize', '200');

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
      .get<{ total: number; items: EntryHistoryItem[] }>(`${environment.apiUrl}/entry`, { params })
      .subscribe({
        next: (response) => {
          this.total.set(response.total);
          this.items.set(response.items);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.messages.fromHttpError(err, 'CHILDREN.HISTORY_LOAD_FAILED');
        }
      });
  }

  clearDates(): void {
    this.from = '';
    this.to = '';
    this.type = '';
    this.load();
  }
}
