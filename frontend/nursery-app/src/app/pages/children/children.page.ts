import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';
import {
  AppSettingsService,
  CodeDataType,
  PrintCardLayout,
  PrintLayoutsDocument
} from '../../core/settings/app-settings.service';
import { PrintLabelComponent, PrintLabelData } from '../../shared/print-label/print-label.component';

interface ChildRow {
  id: string;
  code: string;
  fullName: string;
  guardianName: string;
  guardianPhone?: string;
  guardianRelation?: string;
  className?: string;
  pricePlanName?: string;
  qrCode: string;
  barcode: string;
  entryQrCode?: string;
  entryBarcode?: string;
  photoPath?: string;
  isActive: boolean;
}

@Component({
  selector: 'app-children-page',
  imports: [PageHeaderComponent, TranslatePipe, RouterLink, PrintLabelComponent],
  templateUrl: './children.page.html',
  styleUrl: './children.page.scss'
})
export class ChildrenPage implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly settingsApi = inject(AppSettingsService);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly rows = signal<ChildRow[]>([]);
  readonly cardOpen = signal(false);
  readonly pickupOpen = signal(false);
  readonly cardChild = signal<ChildRow | null>(null);
  readonly activeLayout = signal<PrintCardLayout | null>(null);
  readonly labelData = signal<PrintLabelData>({});
  readonly canWrite = this.auth.hasPermission('children.write');
  readonly canViewEntry = this.auth.hasPermission('entry.scan');

  private layouts: PrintLayoutsDocument = this.settingsApi.defaultLayouts();
  private codeDataType: CodeDataType = 'SystemCode';

  ngOnInit(): void {
    this.settingsApi.load().subscribe({
      next: (settings) => {
        this.layouts = settings.printLayouts ?? this.settingsApi.defaultLayouts();
        this.codeDataType = settings.codeDataType ?? 'SystemCode';
      }
    });
    this.reload();
  }

  openIdentityCard(row: ChildRow): void {
    this.cardChild.set(row);
    this.activeLayout.set(structuredClone(this.layouts.normalCard));
    this.labelData.set(this.buildData(row, 'identity'));
    this.cardOpen.set(true);
    this.pickupOpen.set(false);
  }

  openPickupPass(row: ChildRow): void {
    if (!row.entryQrCode) {
      this.messages.errorKey('CHILDREN.NO_ENTRY_CODE');
      return;
    }
    this.cardChild.set(row);
    this.activeLayout.set(structuredClone(this.layouts.pickupPass));
    this.labelData.set(this.buildData(row, 'entry'));
    this.pickupOpen.set(true);
    this.cardOpen.set(false);
  }

  closeCard(): void {
    this.cardOpen.set(false);
    this.pickupOpen.set(false);
    this.cardChild.set(null);
    this.activeLayout.set(null);
  }

  printCard(): void {
    const body = document.body;
    body.classList.add('nm-printing-label');

    const cleanup = () => {
      body.classList.remove('nm-printing-label');
      window.removeEventListener('afterprint', cleanup);
    };

    window.addEventListener('afterprint', cleanup);
    window.print();
  }

  remove(row: ChildRow): void {
    if (!window.confirm(`Delete child "${row.fullName}"?`)) {
      return;
    }
    this.http.delete(`${environment.apiUrl}/children/${row.id}`).subscribe({
      next: () => this.reload(),
      error: (err) => this.messages.fromHttpError(err)
    });
  }

  private buildData(child: ChildRow, mode: 'identity' | 'entry'): PrintLabelData {
    const systemCode =
      mode === 'entry'
        ? child.entryQrCode || child.entryBarcode || child.code || ''
        : child.qrCode || child.barcode || child.code || '';

    const encoded =
      this.codeDataType === 'ChildCode'
        ? child.code
        : this.codeDataType === 'ChildCodeAndName'
          ? `${child.code}|${child.fullName}`
          : systemCode;

    return {
      fullName: child.fullName,
      code: child.code,
      className: child.className || '',
      guardianName: child.guardianName || '',
      guardianPhone: child.guardianPhone || '',
      pricePlanName: child.pricePlanName || '',
      systemCode: encoded,
      entryCode:
        this.codeDataType === 'SystemCode'
          ? child.entryQrCode || child.entryBarcode || ''
          : encoded,
      photoPath: child.photoPath || '',
      imagePath: child.photoPath || '',
      photo: child.photoPath || '',
      image: child.photoPath || ''
    };
  }

  private reload(): void {
    this.loading.set(true);
    this.http.get<ChildRow[]>(`${environment.apiUrl}/children`).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
