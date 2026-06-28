import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, finalize, map, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.models';
import {
  AuthSession,
  AuthenticatedUser,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  MfaSetupResponse,
  MfaVerifyRequest,
  RefreshTokenRequest,
  ResetPasswordRequest
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'tax-compliance-auth-session';
  private readonly sessionSignal = signal<AuthSession | null>(this.readStoredSession());
  private refreshInFlight: Observable<LoginResponse> | null = null;

  constructor(private readonly httpClient: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.httpClient
      .post<ApiResponse<LoginResponse>>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(
        map((response) => response.data),
        tap((session) => {
          if (!session.requiresMfa) {
            this.storeSession(session);
          }
        })
      );
  }

  refreshToken(refreshToken: string): Observable<LoginResponse> {
    if (!this.refreshInFlight) {
      const request: RefreshTokenRequest = { refreshToken };
      this.refreshInFlight = this.httpClient
        .post<ApiResponse<LoginResponse>>(`${environment.apiBaseUrl}/auth/refresh`, request)
        .pipe(
          map((response) => response.data),
          tap((session) => this.storeSession(session)),
          finalize(() => {
            this.refreshInFlight = null;
          }),
          shareReplay(1)
        );
    }

    return this.refreshInFlight;
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<ApiResponse<object>> {
    return this.httpClient.post<ApiResponse<object>>(`${environment.apiBaseUrl}/auth/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<ApiResponse<object>> {
    return this.httpClient.post<ApiResponse<object>>(`${environment.apiBaseUrl}/auth/reset-password`, request);
  }

  setupMfa(): Observable<MfaSetupResponse> {
    return this.httpClient
      .post<ApiResponse<MfaSetupResponse>>(`${environment.apiBaseUrl}/auth/mfa/setup`, {})
      .pipe(map((response) => response.data));
  }

  enableMfa(request: MfaVerifyRequest): Observable<ApiResponse<object>> {
    return this.httpClient.post<ApiResponse<object>>(`${environment.apiBaseUrl}/auth/mfa/enable`, request);
  }

  disableMfa(request: MfaVerifyRequest): Observable<ApiResponse<object>> {
    return this.httpClient.post<ApiResponse<object>>(`${environment.apiBaseUrl}/auth/mfa/disable`, request);
  }

  getCurrentUser(): Observable<AuthenticatedUser> {
    return this.httpClient
      .get<ApiResponse<AuthenticatedUser>>(`${environment.apiBaseUrl}/auth/me`)
      .pipe(map((response) => response.data));
  }

  logout(): void {
    this.refreshInFlight = null;
    localStorage.removeItem(this.storageKey);
    this.sessionSignal.set(null);
  }

  isAuthenticated(): boolean {
    const session = this.sessionSignal();
    if (!session) {
      return false;
    }

    if (new Date(session.expiresUtc).getTime() > Date.now()) {
      return true;
    }

    return !!session.refreshToken;
  }

  hasRole(role: string): boolean {
    return this.sessionSignal()?.roles.includes(role) ?? false;
  }

  getSession(): AuthSession | null {
    return this.sessionSignal();
  }

  getOrganizationId(): string | undefined {
    return this.sessionSignal()?.organizationId;
  }

  getUserId(): string | undefined {
    return this.sessionSignal()?.userId;
  }

  private storeSession(response: LoginResponse): void {
    const session: AuthSession = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresUtc: response.expiresUtc,
      userId: response.userId,
      email: response.email,
      displayName: response.displayName,
      organizationId: response.organizationId,
      roles: response.roles
    };

    localStorage.setItem(this.storageKey, JSON.stringify(session));
    this.sessionSignal.set(session);
  }

  private readStoredSession(): AuthSession | null {
    const rawValue = localStorage.getItem(this.storageKey);
    return rawValue ? (JSON.parse(rawValue) as AuthSession) : null;
  }
}
