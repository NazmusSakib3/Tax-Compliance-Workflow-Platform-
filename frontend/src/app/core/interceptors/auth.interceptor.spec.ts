import { HttpClient, HttpHeaders, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { OrganizationContextService } from '../services/organization-context.service';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;
  let authService: jasmine.SpyObj<AuthService>;
  let organizationContextService: jasmine.SpyObj<OrganizationContextService>;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getSession']);
    organizationContextService = jasmine.createSpyObj<OrganizationContextService>(
      'OrganizationContextService',
      ['reset'],
      { selectedOrganizationId: signal<string | null>(null) }
    );
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService },
        { provide: OrganizationContextService, useValue: organizationContextService }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should attach the bearer token when a session exists', () => {
    authService.getSession.and.returnValue({
      accessToken: 'token-123',
      refreshToken: 'refresh-123',
      expiresUtc: new Date(Date.now() + 60_000).toISOString(),
      userId: 'user-1',
      email: 'admin@taxplatform.local',
      displayName: 'Admin',
      roles: ['Admin']
    });

    httpClient.get('/api/test').subscribe();

    const request = httpTestingController.expectOne('/api/test');
    expect(request.request.headers.get('Authorization')).toBe('Bearer token-123');
    request.flush({});
  });

  it('should leave requests unchanged when no session exists', () => {
    authService.getSession.and.returnValue(null);

    httpClient.get('/api/test').subscribe();

    const request = httpTestingController.expectOne('/api/test');
    expect(request.request.headers.has('Authorization')).toBeFalse();
    request.flush({});
  });

  it('should attach the selected organization context header for platform admins', () => {
    authService.getSession.and.returnValue({
      accessToken: 'token-123',
      refreshToken: 'refresh-123',
      expiresUtc: new Date(Date.now() + 60_000).toISOString(),
      userId: 'user-1',
      email: 'admin@taxplatform.local',
      displayName: 'Admin',
      roles: ['Admin']
    });
    Object.defineProperty(organizationContextService, 'selectedOrganizationId', {
      value: signal('org-123')
    });

    httpClient.get('/api/test').subscribe();

    const request = httpTestingController.expectOne('/api/test');
    expect(request.request.headers.get('X-Organization-Id')).toBe('org-123');
    request.flush({});
  });
});
