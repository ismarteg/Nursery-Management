import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export type PrintCodeMode = 'BarcodeOnly' | 'QrOnly' | 'Both';
export type CodeDataType = 'SystemCode' | 'ChildCode' | 'ChildCodeAndName';
export type BarcodeFormatType = 'CODE128' | 'CODE39';
export type PrintFieldType = 'text' | 'static' | 'qr' | 'barcode' | 'image';
export type PrintCardType = 'NormalCard' | 'PickupPass';

export interface PrintFieldLayout {
  id: string;
  fieldType: PrintFieldType | string;
  dbField: string;
  staticText?: string | null;
  fontSize: number;
  fontWeight: string;
  color: string;
  align: string;
  xMm: number;
  yMm: number;
  widthMm: number;
  heightMm: number;
  codeFormat?: string | null;
}

export interface PrintCardLayout {
  type: PrintCardType | string;
  widthMm: number;
  heightMm: number;
  backgroundColor: string;
  fields: PrintFieldLayout[];
}

export interface PrintLayoutsDocument {
  normalCard: PrintCardLayout;
  pickupPass: PrintCardLayout;
}

export interface AppSettings {
  id: string;
  identityLabelSize: string;
  pickupLabelSize: string;
  printCodeMode: PrintCodeMode;
  qrSizePx: number;
  barcodeHeight: number;
  barcodeFormat: BarcodeFormatType;
  codeDataType: CodeDataType;
  printLayouts: PrintLayoutsDocument;
  allowedLabelSizes: string[];
  allowedQrSizes: number[];
  allowedBarcodeHeights: number[];
  allowedBarcodeFormats: string[];
  allowedCodeDataTypes: string[];
  availableDbFields: string[];
  updatedAt: string;
}

export interface UpdateSettingsRequest {
  identityLabelSize: string;
  pickupLabelSize: string;
  printCodeMode: PrintCodeMode;
  qrSizePx: number;
  barcodeHeight: number;
  barcodeFormat: BarcodeFormatType;
  codeDataType: CodeDataType;
  printLayouts?: PrintLayoutsDocument;
}

@Injectable({ providedIn: 'root' })
export class AppSettingsService {
  private readonly http = inject(HttpClient);
  readonly current = signal<AppSettings | null>(null);

  load(): Observable<AppSettings> {
    return this.http.get<AppSettings>(`${environment.apiUrl}/settings`).pipe(
      tap((settings) => this.current.set(settings))
    );
  }

  save(request: UpdateSettingsRequest): Observable<AppSettings> {
    return this.http.put<AppSettings>(`${environment.apiUrl}/settings`, request).pipe(
      tap((settings) => this.current.set(settings))
    );
  }

  savePrintLayouts(printLayouts: PrintLayoutsDocument): Observable<AppSettings> {
    return this.http
      .put<AppSettings>(`${environment.apiUrl}/settings/print-layouts`, { printLayouts })
      .pipe(tap((settings) => this.current.set(settings)));
  }

  parseSize(size: string): { widthMm: number; heightMm: number } {
    const [w, h] = (size || '70x40').toLowerCase().split('x');
    return {
      widthMm: Number(w) || 70,
      heightMm: Number(h) || 40
    };
  }

  defaultLayouts(): PrintLayoutsDocument {
    return {
      normalCard: {
        type: 'NormalCard',
        widthMm: 70,
        heightMm: 40,
        backgroundColor: '#ffffff',
        fields: [
          this.field('static', 'static', 'Nursery', 9, 2, 2, 66, 5),
          this.field('text', 'fullName', null, 12, 2, 8, 66, 7, 'bold'),
          this.field('text', 'code', null, 9, 2, 15, 40, 5, 'normal', 'start'),
          this.field('text', 'className', null, 9, 42, 15, 26, 5, 'normal', 'end'),
          this.field('qr', 'systemCode', null, 10, 8, 21, 18, 18),
          this.field('barcode', 'systemCode', null, 8, 30, 22, 36, 14, 'normal', 'center', 'CODE128')
        ]
      },
      pickupPass: {
        type: 'PickupPass',
        widthMm: 50,
        heightMm: 30,
        backgroundColor: '#ffffff',
        fields: [
          this.field('static', 'static', 'Pickup', 8, 2, 1.5, 46, 4),
          this.field('text', 'fullName', null, 10, 2, 6, 46, 5, 'bold'),
          this.field('text', 'code', null, 8, 2, 11, 46, 4),
          this.field('qr', 'entryCode', null, 8, 16, 15, 14, 14)
        ]
      }
    };
  }

  private field(
    fieldType: PrintFieldType,
    dbField: string,
    staticText: string | null,
    fontSize: number,
    xMm: number,
    yMm: number,
    widthMm: number,
    heightMm: number,
    fontWeight = 'normal',
    align = 'center',
    codeFormat?: string
  ): PrintFieldLayout {
    return {
      id: crypto.randomUUID().replace(/-/g, '').slice(0, 8),
      fieldType,
      dbField,
      staticText,
      fontSize,
      fontWeight,
      color: fieldType === 'static' ? '#4a3480' : '#111827',
      align,
      xMm,
      yMm,
      widthMm,
      heightMm,
      codeFormat: codeFormat ?? null
    };
  }
}
