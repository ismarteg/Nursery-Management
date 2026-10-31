import { Component, DestroyRef, ElementRef, HostListener, computed, effect, inject, signal, viewChild } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../core/auth/auth.service';
import { LanguageService } from '../core/i18n/language.service';
import { SiteFooterComponent } from '../shared/site-footer/site-footer.component';

interface NavItem {
  path: string;
  labelKey: string;
  icon: string;
  exact?: boolean;
  visible: () => boolean;
}

interface NavSection {
  id: string;
  labelKey?: string;
  items: NavItem[];
}

@Component({
  selector: 'app-shell-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, SiteFooterComponent],
  templateUrl: './shell.layout.html',
  styleUrl: './shell.layout.scss'
})
export class ShellLayout {
  readonly auth = inject(AuthService);
  readonly language = inject(LanguageService);

  private readonly storageKey = 'nm.sidebar.collapsed';
  private readonly mobileQuery = '(max-width: 860px)';
  private readonly drawerClose = viewChild<ElementRef<HTMLButtonElement>>('drawerClose');
  readonly collapsed = signal(this.readCollapsed());
  readonly isMobile = signal(this.readMobile());
  readonly mobileOpen = signal(false);
  readonly navHidden = computed(() => this.isMobile() && !this.mobileOpen());

  readonly canManageRoles = computed(() => this.auth.hasPermission('roles.manage'));
  readonly canReadAudit = computed(() => this.auth.hasPermission('audit.read'));
  readonly canReadClasses = computed(() => this.auth.hasPermission('classes.read'));
  readonly canReadStaff = computed(() => this.auth.hasPermission('staff.read'));
  readonly canReadChildren = computed(() => this.auth.hasPermission('children.read'));
  readonly canScanEntry = computed(() => this.auth.hasPermission('entry.scan'));
  readonly canReadPayments = computed(() => this.auth.hasPermission('payments.read'));
  readonly canReadPricePlans = computed(() => this.auth.hasPermission('priceplans.read'));
  readonly canReadExpenses = computed(() => this.auth.hasPermission('expenses.read'));
  readonly canReadSalaries = computed(() => this.auth.hasPermission('salaries.read'));
  readonly canReadSettings = computed(() => this.auth.hasPermission('settings.read'));
  readonly canReadLogs = computed(() => this.auth.hasPermission('logs.read'));

  readonly navSections = computed<NavSection[]>(() => {
    const sections: NavSection[] = [
      {
        id: 'main',
        items: [
          {
            path: '/',
            labelKey: 'NAV.DASHBOARD',
            icon: 'fa-solid fa-gauge-high',
            exact: true,
            visible: () => true
          }
        ]
      },
      {
        id: 'children',
        labelKey: 'NAV.SECTION_CHILDREN',
        items: [
          {
            path: '/entry',
            labelKey: 'NAV.ENTRY',
            icon: 'fa-solid fa-qrcode',
            visible: () => this.canScanEntry()
          },
          {
            path: '/children',
            labelKey: 'NAV.CHILDREN',
            icon: 'fa-solid fa-children',
            visible: () => this.canReadChildren()
          },
          {
            path: '/classes',
            labelKey: 'NAV.CLASSES',
            icon: 'fa-solid fa-chalkboard',
            visible: () => this.canReadClasses()
          }
        ]
      },
      {
        id: 'hr',
        labelKey: 'NAV.SECTION_HR',
        items: [
          {
            path: '/staff',
            labelKey: 'NAV.STAFF',
            icon: 'fa-solid fa-user-tie',
            visible: () => this.canReadStaff()
          },
          {
            path: '/attendance',
            labelKey: 'NAV.ATTENDANCE',
            icon: 'fa-solid fa-calendar-check',
            visible: () => this.canReadSalaries()
          },
          {
            path: '/salaries',
            labelKey: 'NAV.SALARIES',
            icon: 'fa-solid fa-wallet',
            visible: () => this.canReadSalaries()
          }
        ]
      },
      {
        id: 'accounting',
        labelKey: 'NAV.SECTION_ACCOUNTING',
        items: [
          {
            path: '/price-plans',
            labelKey: 'NAV.PLANS',
            icon: 'fa-solid fa-tags',
            visible: () => this.canReadPricePlans()
          },
          {
            path: '/payments',
            labelKey: 'NAV.PAYMENTS',
            icon: 'fa-solid fa-money-bill-wave',
            visible: () => this.canReadPayments()
          },
          {
            path: '/expenses',
            labelKey: 'NAV.EXPENSES',
            icon: 'fa-solid fa-receipt',
            visible: () => this.canReadExpenses()
          }
        ]
      },
      {
        id: 'settings-users',
        labelKey: 'NAV.SECTION_SETTINGS_USERS',
        items: [
          {
            path: '/profile',
            labelKey: 'NAV.PROFILE',
            icon: 'fa-solid fa-id-card',
            visible: () => true
          },
          {
            path: '/settings',
            labelKey: 'NAV.SETTINGS',
            icon: 'fa-solid fa-gear',
            visible: () => this.canReadSettings()
          },
          {
            path: '/users',
            labelKey: 'NAV.USERS',
            icon: 'fa-solid fa-users',
            visible: () => this.canManageRoles()
          },
          {
            path: '/roles',
            labelKey: 'NAV.ROLES',
            icon: 'fa-solid fa-user-shield',
            visible: () => this.canManageRoles()
          }
        ]
      },
      {
        id: 'system-admin',
        labelKey: 'NAV.SECTION_SYSTEM_ADMIN',
        items: [
          {
            path: '/logs',
            labelKey: 'NAV.LOGS',
            icon: 'fa-solid fa-clipboard-list',
            visible: () => this.canReadLogs()
          },
          {
            path: '/audit',
            labelKey: 'NAV.AUDIT',
            icon: 'fa-solid fa-clock-rotate-left',
            visible: () => this.canReadAudit()
          }
        ]
      }
    ];

    return sections
      .map((section) => ({
        ...section,
        items: section.items.filter((item) => item.visible())
      }))
      .filter((section) => section.items.length > 0);
  });

  readonly collapseIcon = computed(() => {
    const rtl = this.language.current() === 'ar';
    if (this.collapsed()) {
      return rtl ? 'fa-solid fa-angles-left' : 'fa-solid fa-angles-right';
    }
    return rtl ? 'fa-solid fa-angles-right' : 'fa-solid fa-angles-left';
  });

  constructor() {
    if (typeof window === 'undefined') {
      return;
    }

    const query = window.matchMedia(this.mobileQuery);
    const onChange = () => {
      this.isMobile.set(query.matches);
      if (!query.matches) {
        this.mobileOpen.set(false);
      }
    };
    query.addEventListener('change', onChange);

    const router = inject(Router);
    const navigation = router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.mobileOpen.set(false);
      }
    });

    effect(() => {
      document.body.classList.toggle('nm-nav-open', this.mobileOpen());
    });

    inject(DestroyRef).onDestroy(() => {
      query.removeEventListener('change', onChange);
      navigation.unsubscribe();
      document.body.classList.remove('nm-nav-open');
    });
  }

  toggleSidebar(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    localStorage.setItem(this.storageKey, next ? '1' : '0');
  }

  toggleMobileMenu(): void {
    const next = !this.mobileOpen();
    this.mobileOpen.set(next);
    if (next) {
      setTimeout(() => this.drawerClose()?.nativeElement.focus());
    }
  }

  closeMobileMenu(): void {
    this.mobileOpen.set(false);
  }

  logout(): void {
    this.closeMobileMenu();
    this.auth.logout();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.mobileOpen()) {
      this.closeMobileMenu();
    }
  }

  private readCollapsed(): boolean {
    if (typeof localStorage === 'undefined') {
      return true;
    }
    const value = localStorage.getItem(this.storageKey);
    return value === null ? true : value === '1';
  }

  private readMobile(): boolean {
    return typeof window !== 'undefined' && window.matchMedia(this.mobileQuery).matches;
  }
}
