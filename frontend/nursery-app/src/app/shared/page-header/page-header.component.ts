import { Component, HostListener, Input, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/i18n/language.service';

export interface PageBreadcrumb {
  labelKey: string;
  link?: string | string[];
}

@Component({
  selector: 'app-page-header',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss'
})
export class PageHeaderComponent {
  private readonly auth = inject(AuthService);
  readonly language = inject(LanguageService);

  @Input({ required: true }) titleKey!: string;
  @Input() icon = 'fa-solid fa-file-lines';
  @Input() breadcrumbs: PageBreadcrumb[] = [];
  @Input() subtitleKey?: string;

  readonly menuOpen = signal(false);
  readonly user = this.auth.user;
  readonly roleLabel = computed(() => {
    const roles = this.user()?.roles ?? [];
    return roles.length ? roles.join(' · ') : '—';
  });

  toggleMenu(event: Event): void {
    event.stopPropagation();
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.closeMenu();
    this.auth.logout();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.menuOpen()) {
      this.closeMenu();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenu();
  }
}
