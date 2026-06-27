import { AuthService } from '../services/auth.service';
import { defaultAuthenticatedRoute, isContributorOnly, isPlatformAdmin, isViewerOnly } from './role.utils';

describe('role.utils', () => {
  function createAuthService(roles: string[], organizationId?: string): jasmine.SpyObj<AuthService> {
    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['hasRole', 'getOrganizationId']);
    authService.hasRole.and.callFake((role: string) => roles.includes(role));
    authService.getOrganizationId.and.returnValue(organizationId);
    return authService;
  }

  it('detects contributor-only and viewer-only sessions', () => {
    expect(isContributorOnly(createAuthService(['Contributor']))).toBeTrue();
    expect(isViewerOnly(createAuthService(['Viewer']))).toBeTrue();
    expect(isViewerOnly(createAuthService(['Viewer', 'Contributor']))).toBeFalse();
  });

  it('routes contributors to task occurrences by default', () => {
    expect(defaultAuthenticatedRoute(createAuthService(['Contributor']))).toBe('/task-occurrences');
    expect(defaultAuthenticatedRoute(createAuthService(['ComplianceManager']))).toBe('/dashboard');
  });

  it('detects platform admins without tenant assignment', () => {
    expect(isPlatformAdmin(createAuthService(['Admin']))).toBeTrue();
    expect(isPlatformAdmin(createAuthService(['Admin'], 'org-1'))).toBeFalse();
  });
});
