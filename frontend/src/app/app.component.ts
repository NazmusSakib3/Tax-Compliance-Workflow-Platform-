import { Component, HostListener, OnInit, inject, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { OrganizationContextService } from './core/services/organization-context.service';
import { isContributorOnly, isViewerOnly } from './core/utils/role.utils';
import { ThemeService } from './theme/theme.service';
import { ConfirmDialogComponent } from './shared/components/confirm-dialog.component';
import { RouteProgressComponent } from './shared/components/route-progress.component';
import { ToastContainerComponent } from './shared/components/toast-container.component';

interface NavigationItem {
  label: string;
  contributorLabel?: string;
  viewerLabel?: string;
  path: string;
  icon: string;
  description: string;
  contributorDescription?: string;
  viewerDescription?: string;
  roles?: string[];
  hideForContributorOnly?: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgFor, NgIf, ConfirmDialogComponent, RouteProgressComponent, ToastContainerComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly organizationContextService = inject(OrganizationContextService);
  private readonly router = inject(Router);
  protected readonly themeService = inject(ThemeService);

  protected readonly menuOpen = signal(false);

  protected readonly navigationItems: NavigationItem[] = [
    {
      label: 'Dashboard',
      contributorLabel: 'My Workload',
      viewerLabel: 'Overview',
      path: '/dashboard',
      icon: '◫',
      description: 'See overdue, due soon, and in-progress work at a glance.',
      contributorDescription: 'Review your assigned workload and what needs attention first.',
      viewerDescription: 'Read-only summary of overdue, due soon, and completed work.'
    },
    {
      label: 'Organizations',
      path: '/organizations',
      icon: '◎',
      description: 'Manage the top-level companies in the platform.',
      viewerDescription: 'Browse organization records without making changes.',
      roles: ['Admin', 'ComplianceManager', 'Viewer'],
      hideForContributorOnly: true
    },
    {
      label: 'Legal Entities',
      path: '/legal-entities',
      icon: '▣',
      description: 'Track registered entities under each organization.',
      viewerDescription: 'Review legal entities linked to each organization.',
      roles: ['Admin', 'ComplianceManager', 'Viewer'],
      hideForContributorOnly: true
    },
    {
      label: 'Jurisdictions',
      path: '/jurisdictions',
      icon: '◉',
      description: 'Maintain the countries, regions, and filing authorities you report to.',
      viewerDescription: 'Browse jurisdictions used by compliance templates and rules.',
      roles: ['Admin', 'ComplianceManager', 'Viewer'],
      hideForContributorOnly: true
    },
    {
      label: 'Templates',
      path: '/compliance-templates',
      icon: '▤',
      description: 'Store reusable filing templates and reminder settings.',
      viewerDescription: 'Inspect filing templates and reminder settings.',
      roles: ['Admin', 'ComplianceManager', 'Viewer'],
      hideForContributorOnly: true
    },
    {
      label: 'Task Rules',
      path: '/task-rules',
      icon: '⟳',
      description: 'Define how recurring compliance work should be generated.',
      roles: ['Admin', 'ComplianceManager']
    },
    {
      label: 'Task Occurrences',
      contributorLabel: 'My Tasks',
      viewerLabel: 'Tasks',
      path: '/task-occurrences',
      icon: '☑',
      description: 'Work through generated tasks, assignment, comments, and documents.',
      contributorDescription: 'Update assigned work, add notes, and upload supporting documents.',
      viewerDescription: 'Review generated tasks, statuses, and supporting activity.',
      roles: ['Admin', 'ComplianceManager', 'Contributor', 'Viewer']
    },
    {
      label: 'Audit Log',
      path: '/audit-log',
      icon: '≡',
      description: 'Review tracked changes for accountability and audits.',
      viewerDescription: 'Read-only audit history for compliance accountability.',
      roles: ['Admin', 'ComplianceManager', 'Viewer']
    },
    { label: 'Users', path: '/users', icon: '◈', description: 'Invite users and assign platform roles.', roles: ['Admin'] },
    { label: 'Account Security', path: '/account/security', icon: '⛨', description: 'Manage your multi-factor authentication settings.' }
  ];

  protected get visibleNavigationItems(): NavigationItem[] {
    return this.navigationItems.filter((item) => this.canAccessNavigationItem(item));
  }

  protected get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  protected get isPublicView(): boolean {
    return !this.isAuthenticated || ['/login', '/forgot-password', '/reset-password'].some((path) => this.router.url.startsWith(path));
  }

  protected get userInitials(): string {
    const name = this.displayName.trim();
    if (!name || name === 'Guest') {
      return '?';
    }

    const parts = name.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + (parts.at(-1)?.[0] ?? '')).toUpperCase();
    }

    return name.slice(0, 2).toUpperCase();
  }

  protected get displayName(): string {
    return this.authService.getSession()?.displayName ?? 'Guest';
  }

  protected get activeThemeLabel(): string {
    return this.themeService.currentTheme() === 'dark' ? 'Dark mode' : 'Light mode';
  }

  protected get currentYear(): number {
    return new Date().getFullYear();
  }

  protected get canSwitchOrganizations(): boolean {
    return this.organizationContextService.canSwitchOrganizations();
  }

  protected get organizations() {
    return this.organizationContextService.organizations();
  }

  protected get selectedOrganizationId(): string | null {
    return this.organizationContextService.selectedOrganizationId();
  }

  protected get selectedOrganizationName(): string {
    return this.organizationContextService.selectedOrganizationName();
  }

  protected get sidebarIntro(): string {
    if (this.isContributorOnly) {
      return 'Jump into assigned work, update statuses, and upload supporting documents.';
    }

    if (this.isViewerOnly) {
      return 'Browse compliance records and task progress in read-only mode.';
    }

    return 'Pick a section to manage master data, generated work, or audit history.';
  }

  ngOnInit(): void {
    if (this.isAuthenticated) {
      this.organizationContextService.loadOrganizations();
    }

    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => this.closeMenu());
  }

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.closeMenu();
  }

  protected navigationLabel(item: NavigationItem): string {
    if (this.isContributorOnly && item.contributorLabel) {
      return item.contributorLabel;
    }

    if (this.isViewerOnly && item.viewerLabel) {
      return item.viewerLabel;
    }

    return item.label;
  }

  protected navigationDescription(item: NavigationItem): string {
    if (this.isContributorOnly && item.contributorDescription) {
      return item.contributorDescription;
    }

    if (this.isViewerOnly && item.viewerDescription) {
      return item.viewerDescription;
    }

    return item.description;
  }

  protected onOrganizationChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.organizationContextService.setSelectedOrganizationId(select.value || null);
    this.router.navigateByUrl(this.router.url).catch(() => undefined);
  }

  protected logout(): void {
    this.organizationContextService.reset();
    this.authService.logout();
    this.router.navigate(['/login']).catch(() => undefined);
  }

  protected toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  private get isContributorOnly(): boolean {
    return isContributorOnly(this.authService);
  }

  private get isViewerOnly(): boolean {
    return isViewerOnly(this.authService);
  }

  private canAccessNavigationItem(item: NavigationItem): boolean {
    if (item.hideForContributorOnly && this.isContributorOnly) {
      return false;
    }

    return !item.roles || item.roles.some((role) => this.authService.hasRole(role));
  }
}
