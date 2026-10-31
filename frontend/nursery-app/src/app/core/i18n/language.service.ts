import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

const LANG_KEY = 'nm_lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  readonly current = signal<'ar' | 'en'>('ar');

  init(): void {
    const saved = (localStorage.getItem(LANG_KEY) as 'ar' | 'en' | null) ?? 'ar';
    this.translate.addLangs(['ar', 'en']);
    this.translate.setFallbackLang('ar');
    this.use(saved);
  }

  use(lang: 'ar' | 'en'): void {
    this.translate.use(lang);
    this.current.set(lang);
    localStorage.setItem(LANG_KEY, lang);
    const html = document.documentElement;
    html.lang = lang;
    html.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }

  toggle(): void {
    this.use(this.current() === 'ar' ? 'en' : 'ar');
  }
}
