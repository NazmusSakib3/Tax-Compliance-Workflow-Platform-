import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ComplianceTaskOccurrenceListItem } from '../../core/models/task-occurrence.models';
import { AuthService } from '../../core/services/auth.service';
import { TaskOccurrenceApiService } from '../../core/services/task-occurrence-api.service';
import { isContributorOnly } from '../../core/utils/role.utils';
import { taskOccurrenceStatusLabel, taskOccurrenceStatusClass } from '../../core/utils/task-occurrence.utils';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-task-occurrences-page',
  standalone: true,
  imports: [NgFor, NgIf, RouterLink, DatePipe, LoadingStateComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <section class="page-card list-card">
      <div class="page-header">
        <div>
          <p class="eyebrow">Generated work</p>
          <h2 class="section-title">{{ pageTitle }}</h2>
          <p class="section-subtitle">{{ pageSubtitle }}</p>
        </div>
      </div>

      <p class="status-message error-message" *ngIf="errorMessage">{{ errorMessage }}</p>
      <p class="contributor-note" *ngIf="showAssignedOnlyView">{{ assignedOnlyNote }}</p>

      <div class="assignment-toggle" *ngIf="showAssignmentToggle">
        <button type="button" class="secondary-button" [class.active-toggle]="!assignedOnly" (click)="setAssignedOnly(false)">All visible tasks</button>
        <button type="button" class="secondary-button" [class.active-toggle]="assignedOnly" (click)="setAssignedOnly(true)">My assignments</button>
      </div>

      <div class="list-toolbar" *ngIf="!isLoading">
        <label class="sr-only" for="task-occurrences-search">{{ searchPlaceholder }}</label>
        <input #searchBox id="task-occurrences-search" type="search" [placeholder]="searchPlaceholder" [attr.aria-label]="searchPlaceholder" [value]="search" (input)="search = searchBox.value" (keyup.enter)="onSearch()">
        <label class="sr-only" for="task-occurrences-status">Filter by status</label>
        <select id="task-occurrences-status" class="status-filter" [value]="statusFilter" (change)="onStatusChange($event)" aria-label="Filter by status">
          <option value="">All statuses</option>
          <option *ngFor="let option of statusOptions" [value]="option.value">{{ option.label }}</option>
        </select>
        <button type="button" class="secondary-button" (click)="onSearch()">Search</button>
      </div>

      <app-loading-state *ngIf="isLoading" title="Loading task occurrences" subtitle="Pulling the generated compliance workload for this environment."></app-loading-state>

      <div class="table-shell" *ngIf="!isLoading && occurrences.length">
        <table>
          <caption class="sr-only">Task occurrences</caption>
          <thead>
            <tr>
              <th scope="col">Rule</th>
              <th scope="col">Entity</th>
              <th scope="col">Jurisdiction</th>
              <th scope="col">Due Date</th>
              <th scope="col">Status</th>
              <th scope="col" *ngIf="!showAssignedOnlyView">Assigned To</th>
              <th scope="col"><span class="sr-only">Details</span></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let occurrence of occurrences" [class.row-overdue]="dueTone(occurrence) === 'danger'" [class.row-due-soon]="dueTone(occurrence) === 'warning'">
              <td>{{ occurrence.ruleTitle }}</td>
              <td>{{ occurrence.legalEntityName }}</td>
              <td>{{ occurrence.jurisdictionName }}</td>
              <td>
                <span class="due-date">{{ occurrence.dueDate | date:'mediumDate' }}</span>
                <span class="due-hint" [class.danger]="dueTone(occurrence) === 'danger'" [class.warning]="dueTone(occurrence) === 'warning'" *ngIf="dueHint(occurrence)">{{ dueHint(occurrence) }}</span>
              </td>
              <td><span class="status-badge" [class]="statusClass(occurrence.status)">{{ statusLabel(occurrence.status) }}</span></td>
              <td *ngIf="!showAssignedOnlyView">{{ occurrence.assignedToDisplayName || 'Unassigned' }}</td>
              <td><a [routerLink]="['/task-occurrences', occurrence.id]" class="view-link">Open details</a></td>
            </tr>
          </tbody>
        </table>
      </div>

      <app-empty-state *ngIf="!isLoading && !occurrences.length" [title]="emptyTitle" [subtitle]="emptySubtitle" [alignStart]="showOnboardingHints">
        <ul class="onboarding-list" *ngIf="showOnboardingHints">
          <li>Open a task to update its status when you start work.</li>
          <li>Add comments to capture progress notes for reviewers.</li>
          <li>Upload supporting documents before marking a task complete.</li>
        </ul>
      </app-empty-state>

      <app-pagination *ngIf="!isLoading && totalCount > 0"
        [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [pageSize]="pageSize"
        (pageChange)="loadPage($event)" (pageSizeChange)="onPageSizeChange($event)"></app-pagination>
    </section>
  `,
  styles: [`
    .list-card { padding: 24px 28px; }
    .status-message, .loading-state { margin-bottom: 16px; }
    .contributor-note, .assignment-toggle { margin: 0 0 16px; }
    .contributor-note { padding: 12px 16px; border: 1px solid var(--border); border-radius: var(--radius-md); background: var(--primary-soft); color: var(--text); font-size: 0.9375rem; }
    .assignment-toggle { display: flex; gap: 8px; flex-wrap: wrap; }
    .active-toggle { border-color: var(--primary); color: var(--primary); background: var(--primary-soft); }
    .view-link { color: var(--primary); text-decoration: none; font-weight: 600; font-size: 0.9375rem; }
    .view-link:hover { text-decoration: underline; }
    .onboarding-state { text-align: left; }
    .onboarding-list { margin: 12px 0 0; padding-left: 20px; color: var(--text-muted); }
    .status-filter { flex: 0 0 auto; width: auto; min-width: 160px; }
    .due-date { display: block; }
    .due-hint { display: block; margin-top: 2px; font-size: 0.8125rem; color: var(--text-muted); }
    .due-hint.danger { color: var(--danger); font-weight: 600; }
    .due-hint.warning { color: var(--warning); font-weight: 600; }
    tbody tr.row-overdue { box-shadow: inset 3px 0 0 var(--danger); }
    tbody tr.row-due-soon { box-shadow: inset 3px 0 0 var(--warning); }
  `]
})
export class TaskOccurrencesPageComponent implements OnInit {
  private readonly apiService = inject(TaskOccurrenceApiService);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected occurrences: ComplianceTaskOccurrenceListItem[] = [];
  protected errorMessage = '';
  protected isLoading = true;
  protected page = 1;
  protected totalPages = 1;
  protected totalCount = 0;
  protected search = '';
  protected assignedOnly = false;
  protected statusFilter = '';
  protected pageSize = 25;

  protected readonly statusOptions: { value: string; label: string }[] = [
    { value: 'Draft', label: 'Draft' },
    { value: 'Pending', label: 'Pending' },
    { value: 'InProgress', label: 'In Progress' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Overdue', label: 'Overdue' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];

  ngOnInit(): void {
    this.assignedOnly = this.route.snapshot.queryParamMap.get('assignedOnly') === 'true' || this.isContributorOnly;
    this.statusFilter = this.route.snapshot.queryParamMap.get('status') ?? '';
    this.loadPage(1);
  }

  protected onSearch(): void {
    this.loadPage(1);
  }

  protected onStatusChange(event: Event): void {
    this.statusFilter = (event.target as HTMLSelectElement).value;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { status: this.statusFilter || null },
      queryParamsHandling: 'merge'
    });
    this.loadPage(1);
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.loadPage(1);
  }

  protected setAssignedOnly(assignedOnly: boolean): void {
    this.assignedOnly = assignedOnly;
    this.loadPage(1);
  }

  protected get isContributorOnly(): boolean {
    return isContributorOnly(this.authService);
  }

  protected get showAssignmentToggle(): boolean {
    return this.authService.hasRole('Contributor') && !this.isContributorOnly;
  }

  protected get showAssignedOnlyView(): boolean {
    return this.isContributorOnly || this.assignedOnly;
  }

  protected get assignedOnlyNote(): string {
    return this.isContributorOnly
      ? 'Only tasks assigned to you are shown here.'
      : 'Showing only tasks assigned to you.';
  }

  protected get showOnboardingHints(): boolean {
    return this.isContributorOnly || this.assignedOnly;
  }

  protected get pageTitle(): string {
    return this.showAssignedOnlyView ? 'My Assigned Tasks' : 'Task Occurrences';
  }

  protected get pageSubtitle(): string {
    return this.showAssignedOnlyView
      ? 'Update statuses, add progress notes, and upload evidence for your assigned compliance work.'
      : 'Generated compliance work items with status, due dates, and assignment.';
  }

  protected get searchPlaceholder(): string {
    return this.showAssignedOnlyView ? 'Search my assigned tasks...' : 'Search task occurrences...';
  }

  protected get emptyTitle(): string {
    return this.showAssignedOnlyView ? 'No assigned tasks right now' : 'No generated task occurrences yet';
  }

  protected get emptySubtitle(): string {
    return this.showAssignedOnlyView
      ? 'When a compliance manager assigns work to you, it will appear here. You can also check back after your next login.'
      : 'Create recurring task rules or trigger generation manually to see work items appear here.';
  }

  protected loadPage(page: number): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.apiService.getOccurrences({
      page,
      pageSize: this.pageSize,
      search: this.search || undefined,
      status: this.statusFilter || undefined,
      assignedOnly: this.showAssignedOnlyView
    }).subscribe({
      next: (result) => {
        this.occurrences = result.items;
        this.page = result.page;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.message ?? 'Unable to load task occurrences.';
        this.isLoading = false;
      }
    });
  }

  protected statusLabel(status: number): string {
    return taskOccurrenceStatusLabel(status);
  }

  protected statusClass(status: number): string {
    return taskOccurrenceStatusClass(status);
  }

  protected dueTone(occurrence: ComplianceTaskOccurrenceListItem): 'danger' | 'warning' | null {
    const days = this.daysUntilDue(occurrence);
    if (days === null) {
      return null;
    }

    if (days < 0) {
      return 'danger';
    }

    return days <= 7 ? 'warning' : null;
  }

  protected dueHint(occurrence: ComplianceTaskOccurrenceListItem): string {
    const days = this.daysUntilDue(occurrence);
    if (days === null) {
      return '';
    }

    if (days < 0) {
      const overdueBy = Math.abs(days);
      return overdueBy === 1 ? '1 day overdue' : `${overdueBy} days overdue`;
    }

    if (days > 7) {
      return '';
    }

    if (days === 0) {
      return 'Due today';
    }

    if (days === 1) {
      return 'Due tomorrow';
    }

    return `Due in ${days} days`;
  }

  private daysUntilDue(occurrence: ComplianceTaskOccurrenceListItem): number | null {
    if (this.isClosedStatus(occurrence.status) || !occurrence.dueDate) {
      return null;
    }

    const due = new Date(occurrence.dueDate);
    if (Number.isNaN(due.getTime())) {
      return null;
    }

    const today = new Date();
    const dueMidnight = new Date(due.getFullYear(), due.getMonth(), due.getDate());
    const todayMidnight = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const dayMs = 24 * 60 * 60 * 1000;
    return Math.round((dueMidnight.getTime() - todayMidnight.getTime()) / dayMs);
  }

  private isClosedStatus(status: number): boolean {
    return status === 4 || status === 6;
  }
}
