import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { homeRedirectGuard } from './core/guards/home-redirect.guard';
import { roleGuard } from './core/guards/role.guard';
import { AccountSecurityPageComponent } from './features/account/account-security-page.component';
import { ForgotPasswordPageComponent } from './features/auth/forgot-password-page.component';
import { LoginPageComponent } from './features/auth/login-page.component';
import { ResetPasswordPageComponent } from './features/auth/reset-password-page.component';
import { DashboardPageComponent } from './features/dashboard/dashboard-page.component';
import { OrganizationsPageComponent } from './features/organizations/organizations-page.component';
import { LegalEntitiesPageComponent } from './features/legal-entities/legal-entities-page.component';
import { JurisdictionsPageComponent } from './features/jurisdictions/jurisdictions-page.component';
import { ComplianceTemplatesPageComponent } from './features/compliance-templates/compliance-templates-page.component';
import { TaskRulesPageComponent } from './features/task-rules/task-rules-page.component';
import { TaskOccurrencesPageComponent } from './features/task-occurrences/task-occurrences-page.component';
import { TaskOccurrenceDetailPageComponent } from './features/task-occurrences/task-occurrence-detail-page.component';
import { AuditLogPageComponent } from './features/audit-log/audit-log-page.component';
import { UsersPageComponent } from './features/users/users-page.component';

export const appRoutes: Routes = [
  { path: 'login', component: LoginPageComponent },
  { path: 'forgot-password', component: ForgotPasswordPageComponent },
  { path: 'reset-password', component: ResetPasswordPageComponent },
  { path: '', canActivate: [authGuard, homeRedirectGuard], children: [] },
  { path: 'dashboard', component: DashboardPageComponent, canActivate: [authGuard] },
  { path: 'account/security', component: AccountSecurityPageComponent, canActivate: [authGuard] },
  {
    path: 'organizations',
    component: OrganizationsPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceManager', 'Viewer'] }
  },
  {
    path: 'legal-entities',
    component: LegalEntitiesPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceManager', 'Viewer'] }
  },
  {
    path: 'jurisdictions',
    component: JurisdictionsPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceManager', 'Viewer'] }
  },
  {
    path: 'compliance-templates',
    component: ComplianceTemplatesPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceManager', 'Viewer'] }
  },
  {
    path: 'task-rules',
    component: TaskRulesPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceManager'] }
  },
  {
    path: 'task-occurrences',
    component: TaskOccurrencesPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceManager', 'Contributor', 'Viewer'] }
  },
  {
    path: 'task-occurrences/:id',
    component: TaskOccurrenceDetailPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceManager', 'Contributor', 'Viewer'] }
  },
  {
    path: 'audit-log',
    component: AuditLogPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin', 'ComplianceManager', 'Viewer'] }
  },
  {
    path: 'users',
    component: UsersPageComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] }
  },
  { path: '**', redirectTo: 'dashboard' }
];
