import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import {
  PrintCardLayout,
  PrintFieldLayout,
  PrintFieldType,
  PrintLayoutsDocument
} from '../../core/settings/app-settings.service';
import { PrintLabelComponent } from '../print-label/print-label.component';

@Component({
  selector: 'app-print-layout-editor',
  imports: [CommonModule, FormsModule, TranslatePipe, PrintLabelComponent],
  templateUrl: './print-layout-editor.component.html',
  styleUrl: './print-layout-editor.component.scss'
})
export class PrintLayoutEditorComponent {
  private applyingExternal = true;

  @Input({ required: true }) set layouts(value: PrintLayoutsDocument) {
    if (!this.applyingExternal) {
      this.applyingExternal = true;
      return;
    }
    this.doc.set(structuredClone(value));
    this.selectedFieldId.set(null);
  }

  @Input() dbFields: string[] = [
    'fullName',
    'code',
    'className',
    'guardianName',
    'guardianPhone',
    'pricePlanName',
    'systemCode',
    'entryCode',
    'photoPath',
    'static'
  ];

  @Input() readonly = false;
  @Output() readonly layoutsChange = new EventEmitter<PrintLayoutsDocument>();

  readonly doc = signal<PrintLayoutsDocument | null>(null);
  readonly activeType = signal<'normalCard' | 'pickupPass'>('normalCard');
  readonly selectedFieldId = signal<string | null>(null);
  readonly scale = 4; // px per mm for editor canvas

  readonly activeLayout = computed(() => {
    const d = this.doc();
    if (!d) {
      return null;
    }
    return this.activeType() === 'normalCard' ? d.normalCard : d.pickupPass;
  });

  readonly selectedField = computed(() => {
    const layout = this.activeLayout();
    const id = this.selectedFieldId();
    if (!layout || !id) {
      return null;
    }
    return layout.fields.find((f) => f.id === id) ?? null;
  });

  readonly sampleData = {
    fullName: 'Sara Ahmed',
    code: 'CHD-000001',
    className: 'Butterflies',
    guardianName: 'Ahmed',
    guardianPhone: '01000000000',
    pricePlanName: 'Monthly',
    systemCode: 'SYS-DEMO-001',
    entryCode: 'ENT-DEMO-001',
    photoPath: ''
  };

  readonly fieldTypes: PrintFieldType[] = ['text', 'static', 'qr', 'barcode', 'image'];

  /** Physical alignment — independent of app RTL/LTR. */
  physicalAlign(align: string | undefined): 'left' | 'right' | 'center' {
    const a = (align || 'center').toLowerCase();
    if (a === 'start' || a === 'left') {
      return 'left';
    }
    if (a === 'end' || a === 'right') {
      return 'right';
    }
    return 'center';
  }

  setType(type: 'normalCard' | 'pickupPass'): void {
    this.activeType.set(type);
    this.selectedFieldId.set(null);
  }

  selectField(id: string, event?: Event): void {
    event?.stopPropagation();
    this.selectedFieldId.set(id);
  }

  clearSelection(): void {
    this.selectedFieldId.set(null);
  }

  addField(): void {
    if (this.readonly) {
      return;
    }
    const layout = this.activeLayout();
    if (!layout) {
      return;
    }
    const field: PrintFieldLayout = {
      id: crypto.randomUUID().replace(/-/g, '').slice(0, 8),
      fieldType: 'text',
      dbField: 'fullName',
      staticText: '',
      fontSize: 10,
      fontWeight: 'normal',
      color: '#111827',
      align: 'center',
      xMm: 4,
      yMm: 4,
      widthMm: 30,
      heightMm: 6,
      codeFormat: null
    };
    layout.fields = [...layout.fields, field];
    this.selectedFieldId.set(field.id);
    this.emit();
  }

  removeSelected(): void {
    if (this.readonly) {
      return;
    }
    const layout = this.activeLayout();
    const id = this.selectedFieldId();
    if (!layout || !id) {
      return;
    }
    layout.fields = layout.fields.filter((f) => f.id !== id);
    this.selectedFieldId.set(null);
    this.emit();
  }

  onCardSizeChange(): void {
    this.emit();
  }

  onFieldPropChange(): void {
    this.emit();
  }

  onCanvasPointerDown(event: PointerEvent): void {
    if (this.readonly) {
      return;
    }
    const target = event.target as HTMLElement;
    const fieldEl = target.closest('.editor-field') as HTMLElement | null;
    if (!fieldEl) {
      this.clearSelection();
      return;
    }

    const id = fieldEl.dataset['id'];
    if (!id) {
      return;
    }
    this.selectField(id);
    const field = this.selectedField();
    const layout = this.activeLayout();
    if (!field || !layout) {
      return;
    }

    const startX = event.clientX;
    const startY = event.clientY;
    const originX = field.xMm;
    const originY = field.yMm;

    const onMove = (ev: PointerEvent) => {
      const dx = (ev.clientX - startX) / this.scale;
      const dy = (ev.clientY - startY) / this.scale;
      field.xMm = Math.max(0, Math.min(layout.widthMm - field.widthMm, +(originX + dx).toFixed(1)));
      field.yMm = Math.max(0, Math.min(layout.heightMm - field.heightMm, +(originY + dy).toFixed(1)));
      this.emit(false);
    };

    const onUp = () => {
      window.removeEventListener('pointermove', onMove);
      window.removeEventListener('pointerup', onUp);
      this.emit();
    };

    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp);
  }

  private emit(clone = true): void {
    const d = this.doc();
    if (!d) {
      return;
    }
    this.applyingExternal = false;
    this.layoutsChange.emit(clone ? structuredClone(d) : d);
  }
}
