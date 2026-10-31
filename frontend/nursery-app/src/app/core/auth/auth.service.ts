import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResponse, UserInfo } from './auth.models';

const TOKEN_KEY = 'nm_token';
const USER_KEY = 'nm_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly userSignal = signal<UserInfo | null>(this.readUser());
  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.getToken() && !!this.userSignal());

  login(userName: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { userName, password })
      .pipe(
        tap((response) => {
          localStorage.setItem(TOKEN_KEY, response.token);
          localStorage.setItem(USER_KEY, JSON.stringify(response.user));
          this.userSignal.set(response.user);
        })
      );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.userSignal.set(null);
    void this.router.navigateByUrl('/login');
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  hasPermission(permission: string): boolean {
    const user = this.userSignal();
    if (!user) {
      return false;
    }
    return user.roles.includes('Admin') || user.permissions.includes(permission);
  }

  private readUser(): UserInfo | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as UserInfo;
    } catch {
      return null;
    }
  }
}
