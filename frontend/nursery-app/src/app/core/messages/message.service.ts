import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type MessageType = 'success' | 'error' | 'info' | 'warning';

export interface AppMessage {
  id: number;
  type: MessageType;
  title: string;
  text: string;
}

@Injectable({ providedIn: 'root' })
export class MessageService {
  private readonly translate = inject(TranslateService);
  private seq = 0;

  readonly current = signal<AppMessage | null>(null);

  success(text: string, titleKey = 'MSG.SUCCESS'): void {
    this.show('success', text, titleKey);
  }

  successKey(messageKey: string, titleKey = 'MSG.SUCCESS'): void {
    this.showKey('success', messageKey, titleKey);
  }

  error(text: string, titleKey = 'MSG.ERROR'): void {
    this.show('error', text, titleKey);
  }

  errorKey(messageKey: string, titleKey = 'MSG.ERROR'): void {
    this.showKey('error', messageKey, titleKey);
  }

  info(text: string, titleKey = 'MSG.INFO'): void {
    this.show('info', text, titleKey);
  }

  infoKey(messageKey: string, titleKey = 'MSG.INFO'): void {
    this.showKey('info', messageKey, titleKey);
  }

  warning(text: string, titleKey = 'MSG.WARNING'): void {
    this.show('warning', text, titleKey);
  }

  /** Show API/business errors in the modal. Returns false when only field-validation bag (keep on form). */
  fromHttpError(err: unknown, fallbackKey = 'MSG.GENERIC_ERROR'): boolean {
    const parsed = this.readHttpError(err);
    if (parsed?.isValidation) {
      return false;
    }

    if (parsed?.text) {
      this.error(parsed.text);
      return true;
    }

    this.errorKey(fallbackKey);
    return true;
  }

  /** Field/model validation text for inline form display (no modal). */
  validationText(err: unknown): string | null {
    const parsed = this.readHttpError(err);
    return parsed?.isValidation ? parsed.text : null;
  }

  close(): void {
    this.current.set(null);
  }

  private show(type: MessageType, text: string, titleKey: string): void {
    const title = this.translate.instant(titleKey);
    this.current.set({
      id: ++this.seq,
      type,
      title: title === titleKey ? titleKey : title,
      text: text?.trim() || this.translate.instant('MSG.GENERIC_ERROR')
    });
  }

  private showKey(type: MessageType, messageKey: string, titleKey: string): void {
    const text = this.translate.instant(messageKey);
    this.show(type, text === messageKey ? messageKey : text, titleKey);
  }

  private readHttpError(err: unknown): { text: string | null; isValidation: boolean } | null {
    const error = (err as { error?: unknown })?.error;
    if (typeof error === 'string' && error.trim()) {
      return { text: error.trim(), isValidation: false };
    }

    if (error && typeof error === 'object') {
      const body = error as {
        message?: string;
        title?: string;
        detail?: string;
        errors?: Record<string, string[] | string>;
      };

      if (body.errors && Object.keys(body.errors).length > 0) {
        const parts = Object.entries(body.errors).flatMap(([key, value]) => {
          const messages = Array.isArray(value) ? value : [value];
          return messages.map((msg) => `${key}: ${msg}`);
        });
        // Model/field validation → leave on the form, no modal.
        return { text: parts.join(' · '), isValidation: true };
      }

      if (body.message?.trim()) {
        return { text: body.message.trim(), isValidation: false };
      }

      if (body.detail?.trim()) {
        return { text: body.detail.trim(), isValidation: false };
      }

      if (body.title?.trim()) {
        return { text: body.title.trim(), isValidation: false };
      }
    }

    const status = (err as { status?: number })?.status;
    if (status === 0) {
      return { text: this.translate.instant('MSG.NETWORK_ERROR'), isValidation: false };
    }

    return null;
  }
}
