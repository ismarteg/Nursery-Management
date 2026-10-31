import { MessageService } from '../../core/messages/message.service';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import {
  AppSettingsService,
  BarcodeFormatType,
  CodeDataType,
  PrintCodeMode,
  PrintLayoutsDocument,
  UpdateSettingsRequest
} from '../../core/settings/app-settings.service';
import { AuthService } from '../../core/auth/auth.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { PrintLayoutEditorComponent } from '../../shared/print-layout-editor/print-layout-editor.component';

@Component({
  selector: 'app-settings-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, PrintLayoutEditorComponent],
  templateUrl: './settings.page.html',
  styleUrl: './settings.page.scss'
})
export class SettingsPage implements OnInit {
  private readonly settingsApi = inject(AppSettingsService);
  readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly savingLayouts = signal(false);
  readonly error = signal<string | null>(null);
  readonly allowedSizes = signal<string[]>([]);
  readonly allowedQrSizes = signal<number[]>([80, 100, 120, 140, 160, 180, 200, 240]);
  readonly allowedBarcodeHeights = signal<number[]>([24, 32, 40, 48, 56, 64, 80]);
  readonly availableDbFields = signal<string[]>([]);
  readonly printLayouts = signal<PrintLayoutsDocument | null>(null);
  readonly canWrite = this.auth.hasPermission('settings.write');

  form: UpdateSettingsRequest = {
    identityLabelSize: '70x40',
    pickupLabelSize: '50x30',
    printCodeMode: 'Both',
    qrSizePx: 140,
    barcodeHeight: 40,
    barcodeFormat: 'CODE128',
    codeDataType: 'SystemCode'
  };

  readonly codeModes: PrintCodeMode[] = ['BarcodeOnly', 'QrOnly', 'Both'];
  readonly barcodeFormats: BarcodeFormatType[] = ['CODE128', 'CODE39'];
  readonly codeDataTypes: CodeDataType[] = ['SystemCode', 'ChildCode', 'ChildCodeAndName'];

  ngOnInit(): void {
    this.settingsApi.load().subscribe({
      next: (settings) => {
        this.form = {
          identityLabelSize: settings.identityLabelSize,
          pickupLabelSize: settings.pickupLabelSize,
          printCodeMode: settings.printCodeMode,
          qrSizePx: settings.qrSizePx ?? 140,
          barcodeHeight: settings.barcodeHeight ?? 40,
          barcodeFormat: settings.barcodeFormat ?? 'CODE128',
          codeDataType: settings.codeDataType ?? 'SystemCode'
        };
        this.allowedSizes.set(settings.allowedLabelSizes ?? []);
        if (settings.allowedQrSizes?.length) {
          this.allowedQrSizes.set(settings.allowedQrSizes);
        }
        if (settings.allowedBarcodeHeights?.length) {
          this.allowedBarcodeHeights.set(settings.allowedBarcodeHeights);
        }
        this.availableDbFields.set(settings.availableDbFields ?? []);
        this.printLayouts.set(
          structuredClone(settings.printLayouts ?? this.settingsApi.defaultLayouts())
        );
        this.loading.set(false);
      },
      error: () => {
        this.messages.errorKey('SETTINGS.LOAD_FAILED');
        this.loading.set(false);
      }
    });
  }

  onLayoutsChange(layouts: PrintLayoutsDocument): void {
    this.printLayouts.set(layouts);
    this.form.identityLabelSize = `${Math.round(layouts.normalCard.widthMm)}x${Math.round(layouts.normalCard.heightMm)}`;
    this.form.pickupLabelSize = `${Math.round(layouts.pickupPass.widthMm)}x${Math.round(layouts.pickupPass.heightMm)}`;
  }

  save(): void {
    if (!this.canWrite) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const request: UpdateSettingsRequest = {
      ...this.form,
      printLayouts: this.printLayouts() ?? undefined
    };
    this.settingsApi.save(request).subscribe({
      next: (settings) => {
        this.saving.set(false);
        this.printLayouts.set(structuredClone(settings.printLayouts));
        this.messages.successKey('SETTINGS.SAVED');
      },
      error: (err) => {
        this.saving.set(false);
        if (!this.messages.fromHttpError(err, 'SETTINGS.SAVE_FAILED')) {
          this.error.set(this.messages.validationText(err));
        }
      }
    });
  }

  saveLayoutsOnly(): void {
    if (!this.canWrite || !this.printLayouts()) {
      return;
    }
    this.savingLayouts.set(true);
    this.settingsApi.savePrintLayouts(this.printLayouts()!).subscribe({
      next: (settings) => {
        this.savingLayouts.set(false);
        this.printLayouts.set(structuredClone(settings.printLayouts));
        this.form.identityLabelSize = settings.identityLabelSize;
        this.form.pickupLabelSize = settings.pickupLabelSize;
        this.messages.successKey('SETTINGS.LAYOUTS_SAVED');
      },
      error: (err) => {
        this.savingLayouts.set(false);
        this.messages.fromHttpError(err, 'SETTINGS.SAVE_FAILED');
      }
    });
  }

  resetLayouts(): void {
    if (!this.canWrite) {
      return;
    }
    this.printLayouts.set(this.settingsApi.defaultLayouts());
  }
}
