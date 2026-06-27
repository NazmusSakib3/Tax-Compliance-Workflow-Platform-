import { AuthService } from '../services/auth.service';

export function isContributorOnly(authService: AuthService): boolean {
  return authService.hasRole('Contributor') &&
    !authService.hasRole('Admin') &&
    !authService.hasRole('ComplianceManager');
}

export function isViewerOnly(authService: AuthService): boolean {
  return authService.hasRole('Viewer') &&
    !authService.hasRole('Admin') &&
    !authService.hasRole('ComplianceManager') &&
    !authService.hasRole('Contributor');
}

export function isPlatformAdmin(authService: AuthService): boolean {
  return authService.hasRole('Admin') && !authService.getOrganizationId();
}

export function defaultAuthenticatedRoute(authService: AuthService): string {
  return isContributorOnly(authService) ? '/task-occurrences' : '/dashboard';
}
