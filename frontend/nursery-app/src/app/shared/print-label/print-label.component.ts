import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  QueryList,
  SimpleChanges,
  ViewChildren,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import QRCode from 'qrcode';
import JsBarcode from 'jsbarcode';
import { environment } from '../../../environments/environment';
import { PrintCardLayout, PrintFieldLayout } from '../../core/settings/app-settings.service';

export type PrintLabelData = Record<string, string | null | undefined>;

@Component({
  selector: 'app-print-label',
  imports: [CommonModule],
  templateUrl: './print-label.component.html',
  styleUrl: './print-label.component.scss'
})
export class PrintLabelComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input({ required: true }) layout!: PrintCardLayout;
  @Input() data: PrintLabelData = {};
  @Input() previewScale = 1;
  @Input() printId = 'print-label';

  @ViewChildren('qrCanvas') qrCanvases!: QueryList<ElementRef<HTMLCanvasElement>>;
  @ViewChildren('barcodeSvg') barcodeSvgs!: QueryList<ElementRef<SVGSVGElement>>;

  readonly resolvedImages = signal<Record<string, string>>({});

  private readonly http = inject(HttpClient);
  private ready = false;
  private objectUrls: string[] = [];
  private loadToken = 0;

  ngAfterViewInit(): void {
    this.ready = true;
    this.refresh();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.ready) {
      return;
    }
    if (changes['layout'] || changes['data'] || changes['previewScale']) {
      this.refresh();
    }
  }

  ngOnDestroy(): void {
    this.revokeObjectUrls();
  }

  /** Physical alignment — never flips with page RTL/LTR. */
  physicalAlign(align: string | undefined): string {
    const a = (align || 'center').toLowerCase();
    if (a === 'start' || a === 'left') {
      return 'left';
    }
    if (a === 'end' || a === 'right') {
      return 'right';
    }
    return 'center';
  }

  physicalTextAlign(align: string | undefined): 'left' | 'right' | 'center' {
    return this.physicalAlign(align) as 'left' | 'right' | 'center';
  }

  fieldValue(field: PrintFieldLayout): string {
    if (field.fieldType === 'static') {
      return field.staticText || '';
    }
    const key = field.dbField || '';
    const direct = (this.data[key] ?? '').toString();
    if (direct) {
      return direct;
    }
    // Aliases for image/photo fields
    if (key === 'imagePath' || key === 'photo' || key === 'image') {
      return (this.data['photoPath'] ?? '').toString();
    }
    return '';
  }

  imageSrc(field: PrintFieldLayout): string | null {
    const resolved = this.resolvedImages()[field.id];
    if (resolved) {
      return resolved;
    }
    const raw = this.fieldValue(field);
    if (!raw) {
      return null;
    }
    if (raw.startsWith('http') || raw.startsWith('data:') || raw.startsWith('blob:')) {
      return raw;
    }
    return null;
  }

  private refresh(): void {
    this.loadImages();
    this.renderCodes().catch(() => undefined);
  }

  private loadImages(): void {
    const token = ++this.loadToken;
    this.revokeObjectUrls();
    this.resolvedImages.set({});

    const imageFields = (this.layout?.fields ?? []).filter((f) => f.fieldType === 'image');
    for (const field of imageFields) {
      const raw = this.fieldValue(field);
      if (!raw) {
        continue;
      }

      if (raw.startsWith('http') || raw.startsWith('data:') || raw.startsWith('blob:')) {
        this.resolvedImages.update((map) => ({ ...map, [field.id]: raw }));
        continue;
      }

      const url = `${environment.apiUrl}/files/${raw.replace(/^\/+/, '')}`;
      this.http.get(url, { responseType: 'blob' }).subscribe({
        next: (blob) => {
          if (token !== this.loadToken) {
            return;
          }
          const objectUrl = URL.createObjectURL(blob);
          this.objectUrls.push(objectUrl);
          this.resolvedImages.update((map) => ({ ...map, [field.id]: objectUrl }));
        },
        error: () => undefined
      });
    }
  }

  private revokeObjectUrls(): void {
    for (const url of this.objectUrls) {
      URL.revokeObjectURL(url);
    }
    this.objectUrls = [];
  }

  private async renderCodes(): Promise<void> {
    await Promise.resolve();
    const qrFields = this.layout.fields.filter((f) => f.fieldType === 'qr');
    const barcodeFields = this.layout.fields.filter((f) => f.fieldType === 'barcode');
    const qrEls = this.qrCanvases?.toArray() ?? [];
    const barcodeEls = this.barcodeSvgs?.toArray() ?? [];

    for (let i = 0; i < qrFields.length; i++) {
      const field = qrFields[i];
      const el = qrEls[i]?.nativeElement;
      if (!el) {
        continue;
      }
      const value = this.fieldValue(field);
      if (!value) {
        continue;
      }
      const px = Math.max(40, Math.round(field.widthMm * 3.78 * this.previewScale));
      await QRCode.toCanvas(el, value, { width: px, margin: 0 });
    }

    for (let i = 0; i < barcodeFields.length; i++) {
      const field = barcodeFields[i];
      const el = barcodeEls[i]?.nativeElement;
      if (!el) {
        continue;
      }
      const value = this.fieldValue(field);
      if (!value) {
        continue;
      }
      const formatRaw = (field.codeFormat || 'CODE128').toUpperCase();
      const format =
        formatRaw === 'CODE39' && !/^[0-9A-Z\-. $/+%]+$/i.test(value) ? 'CODE128' : formatRaw;
      const height = Math.max(16, Math.round(field.heightMm * 3.78 * this.previewScale * 0.7));
      JsBarcode(el, value, {
        format,
        displayValue: true,
        fontSize: Math.max(8, Math.round(field.fontSize * this.previewScale)),
        height,
        margin: 0,
        width: Math.max(1, Math.min(2.5, field.widthMm / 20))
      });
    }
  }
}
