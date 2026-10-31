import { MessageService } from '../../core/messages/message.service';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { DatePipe } from '@angular/common';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';
import { environment } from '../../../environments/environment';

interface ScanResult {
  id: string;
  type: string;
  method: string;
  occurredAt: string;
  previousType?: string;
  matchedCodeKind?: 'Entry' | 'Identity';
  child: {
    id: string;
    code: string;
    fullName: string;
    className?: string;
    guardianName: string;
    guardianPhone?: string;
  };
}

interface PresentChild {
  id: string;
  code: string;
  fullName: string;
  className?: string;
  since: string;
}

interface CameraOption {
  id: string;
  label: string;
}

@Component({
  selector: 'app-entry-desk-page',
  imports: [PageHeaderComponent, FormsModule, TranslatePipe, DatePipe, RouterLink],
  templateUrl: './entry-desk.page.html',
  styleUrl: './entry-desk.page.scss'
})
export class EntryDeskPage implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly messages = inject(MessageService);
  private readonly translate = inject(TranslateService);
  private scanner: Html5Qrcode | null = null;
  private session = 0;
  private destroyed = false;
  private lastAccepted = '';
  private lastSeenAt = 0;

  @ViewChild('codeInput') codeInput?: ElementRef<HTMLInputElement>;

  code = '';
  forceType: number | null = null;
  readonly scanning = signal(false);
  readonly error = signal<string | null>(null);
  readonly lastResult = signal<ScanResult | null>(null);
  readonly present = signal<PresentChild[]>([]);
  readonly presentCount = signal(0);
  readonly cameraOn = signal(false);
  readonly cameraBusy = signal(false);
  readonly cameraError = signal<string | null>(null);
  readonly cameras = signal<CameraOption[]>([]);
  readonly cameraId = signal('');

  ngOnInit(): void {
    this.reloadPresent();
    setTimeout(() => this.codeInput?.nativeElement.focus(), 0);
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.session++;
    void this.releaseScanner();
  }

  async startCamera(): Promise<void> {
    if (this.cameraOn() || this.cameraBusy()) {
      return;
    }

    this.cameraError.set(null);
    if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) {
      this.cameraError.set(this.translate.instant('ENTRY.CAMERA_UNSUPPORTED'));
      return;
    }

    const session = ++this.session;
    this.cameraBusy.set(true);
    this.cameraOn.set(true);
    await this.waitForReader();

    try {
      this.scanner = this.createScanner();
      await this.openFirstWorkingCamera();
      if (session !== this.session || this.destroyed) {
        await this.releaseScanner();
        return;
      }
      await this.loadCameraList();
    } catch (err) {
      if (session === this.session) {
        this.cameraOn.set(false);
        this.cameraError.set(this.cameraMessage(err));
      }
      await this.releaseScanner();
    } finally {
      if (session === this.session) {
        this.cameraBusy.set(false);
      }
    }
  }

  async switchCamera(cameraId: string): Promise<void> {
    if (!cameraId || cameraId === this.cameraId() || !this.scanner || this.cameraBusy()) {
      return;
    }

    const previousId = this.cameraId();
    this.cameraBusy.set(true);
    this.cameraError.set(null);
    try {
      await this.releaseScanner();
      this.stopLeftoverTracks();
      await this.delay(300);
      this.scanner = this.createScanner();
      await this.runCamera(cameraId);
      this.cameraId.set(cameraId);
    } catch (err) {
      this.cameraError.set(this.cameraMessage(err));
      await this.releaseScanner();
      this.stopLeftoverTracks();
      if (previousId) {
        try {
          await this.delay(300);
          this.scanner = this.createScanner();
          await this.runCamera(previousId);
          this.cameraId.set(previousId);
          return;
        } catch {
          await this.releaseScanner();
        }
      }
      this.cameraOn.set(false);
    } finally {
      this.cameraBusy.set(false);
    }
  }

  async stopCamera(): Promise<void> {
    this.session++;
    this.cameraOn.set(false);
    this.cameraId.set('');
    this.cameraBusy.set(false);
    await this.releaseScanner();
  }

  scan(method: 0 | 2 = 2): void {
    const value = this.code.trim();
    if (!value || this.scanning()) {
      return;
    }

    this.scanning.set(true);
    this.error.set(null);

    this.http
      .post<ScanResult>(`${environment.apiUrl}/entry/scan`, {
        code: value,
        method,
        forceType: this.forceType
      })
      .subscribe({
        next: (result) => {
          this.lastResult.set(result);
          this.code = '';
          this.scanning.set(false);
          this.reloadPresent();
          if (!this.cameraOn()) {
            this.codeInput?.nativeElement.focus();
          }
        },
        error: (err) => {
          this.scanning.set(false);
          this.messages.fromHttpError(err);
          if (!this.cameraOn()) {
            this.codeInput?.nativeElement.select();
          }
        }
      });
  }

  private createScanner(): Html5Qrcode {
    return new Html5Qrcode('entry-qr-reader', {
      verbose: false,
      formatsToSupport: [Html5QrcodeSupportedFormats.QR_CODE]
    });
  }

  private async openFirstWorkingCamera(): Promise<void> {
    const attempts: Array<string | MediaTrackConstraints> = [
      { facingMode: 'environment' },
      { facingMode: 'user' }
    ];
    let lastError: unknown;

    for (const attempt of attempts) {
      try {
        await this.runCamera(attempt);
        return;
      } catch (err) {
        lastError = err;
        if (this.isPermissionDenied(err)) {
          throw err;
        }
        await this.prepareRetry();
      }
    }

    const devices = await navigator.mediaDevices.enumerateDevices();
    const cameras = devices.filter((device) => device.kind === 'videoinput' && device.deviceId);
    for (const camera of cameras) {
      try {
        await this.runCamera(camera.deviceId);
        return;
      } catch (err) {
        lastError = err;
        await this.prepareRetry();
      }
    }

    throw lastError ?? Object.assign(new Error('No camera'), { name: 'NotFoundError' });
  }

  private async prepareRetry(): Promise<void> {
    this.stopLeftoverTracks();
    await this.releaseScanner();
    this.stopLeftoverTracks();
    await this.delay(350);
    if (!this.destroyed && document.getElementById('entry-qr-reader')) {
      this.scanner = this.createScanner();
    }
  }

  private async runCamera(cameraIdOrConfig: string | MediaTrackConstraints): Promise<void> {
    if (!this.scanner) {
      return;
    }
    if (this.scanner.isScanning) {
      await this.scanner.stop();
      await this.delay(250);
    }

    await this.scanner.start(
      cameraIdOrConfig,
      {
        fps: 10,
        disableFlip: false,
        qrbox: (width, height) => {
          const edge = Math.floor(Math.min(width, height) * 0.7);
          const size = Math.max(50, Math.min(edge || 50, width, height));
          return { width: size, height: size };
        }
      },
      (text) => this.onQr(text),
      () => this.onQrMiss()
    );
    if (typeof cameraIdOrConfig === 'string') {
      this.cameraId.set(cameraIdOrConfig);
    }
  }

  private async loadCameraList(): Promise<void> {
    const devices = await navigator.mediaDevices.enumerateDevices();
    const videos = devices.filter((device) => device.kind === 'videoinput' && device.deviceId);
    this.cameras.set(
      videos.map((device, index) => ({
        id: device.deviceId,
        label: device.label?.trim() || this.translate.instant('ENTRY.CAMERA_N', { n: index + 1 })
      }))
    );

    try {
      const current = this.scanner?.getRunningTrackSettings().deviceId ?? '';
      if (current) {
        this.cameraId.set(current);
      }
    } catch {
      // The stream settings are not ready yet.
    }
  }

  private isPermissionDenied(err: unknown): boolean {
    return /NotAllowed|Permission denied/i.test(this.errorText(err));
  }

  private async waitForReader(): Promise<void> {
    for (let attempt = 0; attempt < 20; attempt++) {
      const reader = document.getElementById('entry-qr-reader');
      if (reader && reader.clientWidth > 0) {
        return;
      }
      await this.delay(50);
    }
  }

  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  private stopLeftoverTracks(): void {
    const root = document.getElementById('entry-qr-reader');
    root?.querySelectorAll('video').forEach((video) => {
      const stream = video.srcObject;
      if (stream instanceof MediaStream) {
        stream.getTracks().forEach((track) => track.stop());
      }
      video.srcObject = null;
    });
  }

  private onQr(text: string): void {
    const value = text.trim();
    if (!value) {
      return;
    }

    const now = Date.now();
    if (value === this.lastAccepted) {
      this.lastSeenAt = now;
      return;
    }
    if (this.scanning()) {
      return;
    }

    this.lastAccepted = value;
    this.lastSeenAt = now;
    this.code = value;
    this.scan(0);
  }

  private onQrMiss(): void {
    if (this.lastAccepted && Date.now() - this.lastSeenAt > 2000) {
      this.lastAccepted = '';
    }
  }

  private cameraMessage(err: unknown): string {
    const text = this.errorText(err);
    if (/NotAllowed|Permission denied|permission/i.test(text)) {
      return this.translate.instant('ENTRY.CAMERA_DENIED');
    }
    if (/NotFound|no camera|not found|Overconstrained/i.test(text)) {
      return this.translate.instant('ENTRY.CAMERA_UNAVAILABLE');
    }
    if (/NotReadable|TrackStart|Could not start|in use|AbortError/i.test(text)) {
      return this.translate.instant('ENTRY.CAMERA_BUSY');
    }
    return this.translate.instant('ENTRY.CAMERA_ERROR');
  }

  private errorText(err: unknown): string {
    if (typeof err === 'string') {
      return err;
    }
    if (err instanceof Error) {
      return `${err.name} ${err.message}`;
    }
    if (err && typeof err === 'object') {
      const named = err as { name?: string; message?: string };
      return `${named.name ?? ''} ${named.message ?? ''}`;
    }
    return '';
  }

  private async releaseScanner(): Promise<void> {
    const scanner = this.scanner;
    this.scanner = null;
    if (!scanner) {
      return;
    }
    try {
      if (scanner.isScanning) {
        await scanner.stop();
      }
      scanner.clear();
    } catch {
      // The camera is already stopped.
    }
  }

  private reloadPresent(): void {
    this.http.get<{ count: number; items: PresentChild[] }>(`${environment.apiUrl}/entry/present`).subscribe({
      next: (response) => {
        this.presentCount.set(response.count);
        this.present.set(response.items);
      }
    });
  }
}
