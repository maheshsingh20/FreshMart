import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthTokens, User } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/auth`;
  // Lazy-injected to avoid circular dependency
  private get notifService() {
    return (window as any).__notifService as import('./notification.service').NotificationService | undefined;
  }

  login(email: string, password: string): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.baseUrl}/login`, { email, password }).pipe(
      tap(tokens => {
        this.storeTokens(tokens);
        this.notifService?.init(tokens.accessToken);
      })
    );
  }

  googleLogin(idToken: string): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.baseUrl}/google`, { idToken }).pipe(
      tap(tokens => {
        this.storeTokens(tokens);
        this.notifService?.init(tokens.accessToken);
      })
    );
  }

  register(data: {
    email: string; password: string; firstName: string;
    lastName: string; phoneNumber?: string; role?: string; inviteCode?: string;
  }): Observable<{ userId: string; email: string; role: string }> {
    return this.http.post<any>(`${this.baseUrl}/register`, data);
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      this.http.post(`${this.baseUrl}/logout`, { refreshToken }).subscribe({ error: () => {} });
    }
    this.notifService?.disconnect();
    this.clearTokens();
    this.router.navigate(['/auth/login']);
  }

  refreshToken(): Observable<AuthTokens> {
    return this.http.post<AuthTokens>(`${this.baseUrl}/refresh`,
      { refreshToken: this.getRefreshToken() }).pipe(
      tap(tokens => this.storeTokens(tokens))
    );
  }

  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/me`);
  }

  getAccessToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    return localStorage.getItem('refresh_token');
  }

  isAuthenticated(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    const token = this.getAccessToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch { return false; }
  }

  getUserName(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      // JwtRegisteredClaimNames.GivenName maps to 'given_name'
      return payload['given_name']
        ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname']
        ?? payload['name']
        ?? null;
    } catch { return null; }
  }

  getUserRole(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? null;
    } catch { return null; }
  }

  private storeTokens(tokens: AuthTokens): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.setItem('access_token', tokens.accessToken);
    localStorage.setItem('refresh_token', tokens.refreshToken);
  }

  private clearTokens(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }
}
