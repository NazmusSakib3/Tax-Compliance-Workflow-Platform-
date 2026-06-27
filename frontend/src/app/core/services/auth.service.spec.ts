import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    service = TestBed.inject(AuthService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('should store the session after a successful login', () => {
    const expiresUtc = new Date(Date.now() + 3_600_000).toISOString();

    service.login({ email: 'admin@taxplatform.local', password: 'Admin123!' }).subscribe((response) => {
      expect(response.accessToken).toBe('jwt-token');
      expect(service.isAuthenticated()).toBeTrue();
      expect(service.hasRole('Admin')).toBeTrue();
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}/auth/login`);
    request.flush({
      success: true,
      message: 'Login successful.',
      data: {
        accessToken: 'jwt-token',
        refreshToken: 'refresh-token',
        expiresUtc,
        userId: 'user-1',
        email: 'admin@taxplatform.local',
        displayName: 'Platform Administrator',
        roles: ['Admin']
      }
    });
  });

  it('should post forgot-password requests without storing a session', () => {
    let responseMessage = '';

    service.forgotPassword({ email: 'user@example.com' }).subscribe((response) => {
      responseMessage = response.message;
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}/auth/forgot-password`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'user@example.com' });
    request.flush({
      success: true,
      message: 'If the account exists, a password reset token has been generated.',
      data: {}
    });

    expect(responseMessage).toContain('If the account exists');
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('should post reset-password requests without storing a session', () => {
    let responseMessage = '';

    service.resetPassword({
      email: 'user@example.com',
      token: 'reset-token',
      newPassword: 'NewPassword123!'
    }).subscribe((response) => {
      responseMessage = response.message;
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}/auth/reset-password`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'user@example.com',
      token: 'reset-token',
      newPassword: 'NewPassword123!'
    });
    request.flush({
      success: true,
      message: 'Password reset successful.',
      data: {}
    });

    expect(responseMessage).toContain('Password reset successful');
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('should not store a session when login requires MFA', () => {
    service.login({ email: 'mfa-user@example.com', password: 'CorrectHorseBatteryStaple1!' }).subscribe();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}/auth/login`);
    request.flush({
      success: true,
      message: 'MFA required.',
      data: {
        requiresMfa: true,
        email: 'mfa-user@example.com',
        accessToken: '',
        refreshToken: '',
        expiresUtc: new Date().toISOString(),
        displayName: '',
        roles: []
      }
    });

    expect(service.isAuthenticated()).toBeFalse();
  });

  it('should treat the session as authenticated when the access token expired but a refresh token exists', () => {
    localStorage.clear();
    const expiredUtc = new Date(Date.now() - 3_600_000).toISOString();
    localStorage.setItem('tax-compliance-auth-session', JSON.stringify({
      accessToken: 'jwt-token',
      refreshToken: 'refresh-token',
      expiresUtc: expiredUtc,
      userId: 'user-1',
      email: 'admin@taxplatform.local',
      displayName: 'Platform Administrator',
      roles: ['Admin']
    }));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    const refreshedService = TestBed.inject(AuthService);
    expect(refreshedService.isAuthenticated()).toBeTrue();
  });

  it('should clear the session on logout', () => {
    const expiresUtc = new Date(Date.now() + 3_600_000).toISOString();

    service.login({ email: 'admin@taxplatform.local', password: 'Admin123!' }).subscribe();
    const loginRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}/auth/login`);
    loginRequest.flush({
      success: true,
      message: 'Login successful.',
      data: {
        accessToken: 'jwt-token',
        refreshToken: 'refresh-token',
        expiresUtc,
        userId: 'user-1',
        email: 'admin@taxplatform.local',
        displayName: 'Platform Administrator',
        roles: ['Admin']
      }
    });

    service.logout();
    expect(service.isAuthenticated()).toBeFalse();
  });
});
