import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { AppComponent } from './app.component';
import { AuthService } from './core/services/auth.service';
import { OrganizationContextService } from './core/services/organization-context.service';

describe('AppComponent', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let organizationContextService: jasmine.SpyObj<OrganizationContextService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getSession', 'hasRole', 'isAuthenticated', 'logout', 'getOrganizationId']);
    organizationContextService = jasmine.createSpyObj<OrganizationContextService>(
      'OrganizationContextService',
      ['loadOrganizations', 'reset', 'setSelectedOrganizationId'],
      {
        canSwitchOrganizations: signal(false),
        organizations: signal([]),
        selectedOrganizationId: signal<string | null>(null),
        selectedOrganizationName: signal('All organizations')
      }
    );
    authService.isAuthenticated.and.returnValue(true);
    authService.getSession.and.returnValue({
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresUtc: new Date(Date.now() + 3_600_000).toISOString(),
      userId: 'user-1',
      email: 'admin@example.com',
      displayName: 'Admin User',
      roles: ['Admin']
    });
    authService.hasRole.and.callFake((role: string) => role === 'Admin');
    authService.getOrganizationId.and.returnValue('org-1');

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: OrganizationContextService, useValue: organizationContextService }
      ]
    }).compileComponents();
  });

  it('should create the app shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const component = fixture.componentInstance;

    expect(component).toBeTruthy();
  });

  it('shows management navigation for admins', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const labels = navLabels(fixture.nativeElement);

    expect(labels).toContain('Task Rules');
    expect(labels).toContain('Audit Log');
    expect(labels).toContain('Users');
    expect(labels).toContain('Task Occurrences');
  });

  it('shows only reachable navigation and task-focused copy for contributors', () => {
    authService.getSession.and.returnValue({
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresUtc: new Date(Date.now() + 3_600_000).toISOString(),
      userId: 'user-1',
      email: 'contributor@example.com',
      displayName: 'Contributor User',
      roles: ['Contributor']
    });
    authService.hasRole.and.callFake((role: string) => role === 'Contributor');

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const labels = navLabels(fixture.nativeElement);

    expect(labels).toContain('My Tasks');
    expect(labels).not.toContain('Organizations');
    expect(labels).not.toContain('Task Rules');
    expect(labels).not.toContain('Audit Log');
    expect(labels).not.toContain('Users');
  });

  it('uses viewer-specific labels for read-only users', () => {
    authService.getSession.and.returnValue({
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresUtc: new Date(Date.now() + 3_600_000).toISOString(),
      userId: 'user-1',
      email: 'viewer@example.com',
      displayName: 'Viewer User',
      roles: ['Viewer']
    });
    authService.hasRole.and.callFake((role: string) => role === 'Viewer');

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const labels = navLabels(fixture.nativeElement);

    expect(labels).toContain('Overview');
    expect(labels).toContain('Tasks');
  });

  function navLabels(nativeElement: HTMLElement): string[] {
    return Array.from(nativeElement.querySelectorAll('.nav-label'))
      .map((element) => element.textContent?.trim() ?? '');
  }
});
