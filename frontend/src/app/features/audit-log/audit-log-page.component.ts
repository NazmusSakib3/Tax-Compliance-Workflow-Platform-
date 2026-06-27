import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GlobalAuditLogEntry } from '../../core/models/audit-log.models';
import { AuditLogApiService } from '../../core/services/audit-log-api.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { PaginationComponent } from '../../shared/components/pagination.component';

@Component({
  selector: 'app-audit-log-page',
  standalone: true,
  imports: [NgFor, NgIf, DatePipe, RouterLink, LoadingStateComponent, EmptyStateComponent, PaginationComponent],
  template: `
    <section class="page-card audit-page">
      <div class="page-header">
        <div>
          <p class="eyebrow">Compliance history</p>
          <h2 class="section-title">Audit Log</h2>
          <p class="section-subtitle">Review tracked workflow changes across all task occurrences.</p>
        </div>
      </div>

      <app-loading-state *ngIf="isLoading" title="Loading audit entries" subtitle="Fetching the latest recorded activity."></app-loading-state>

      <p class="status-message error-message" *ngIf="errorMessage">{{ errorMessage }}</p>

      <div class="table-shell" *ngIf="!isLoading && entries.length">
        <table>
          <caption class="sr-only">Audit log entries</caption>
          <thead>
            <tr>
              <th scope="col">When</th>
              <th scope="col">Action</th>
              <th scope="col">Task</th>
              <th scope="col">Entity</th>
              <th scope="col">Description</th>
              <th scope="col">Performed By</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let entry of entries">
              <td>{{ entry.createdUtc | date: 'medium' }}</td>
              <td>{{ entry.actionType }}</td>
              <td>
                <a [routerLink]="['/task-occurrences', entry.complianceTaskOccurrenceId]">{{ entry.ruleTitle }}</a>
              </td>
              <td>{{ entry.legalEntityName }}</td>
              <td>{{ entry.description }}</td>
              <td>{{ entry.performedByDisplayName }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <app-empty-state *ngIf="!isLoading && !entries.length && !errorMessage"
        title="No audit entries yet"
        subtitle="Workflow changes will appear here once tasks are updated."></app-empty-state>

      <app-pagination *ngIf="!isLoading && totalCount > 0"
        [page]="page" [totalPages]="totalPages" [totalCount]="totalCount" [pageSize]="pageSize"
        (pageChange)="loadPage($event)" (pageSizeChange)="onPageSizeChange($event)"></app-pagination>
    </section>
  `,
  styles: [`
    .audit-page { padding: 24px; }
  `]
})
export class AuditLogPageComponent implements OnInit {
  private readonly auditLogApiService = inject(AuditLogApiService);

  protected entries: GlobalAuditLogEntry[] = [];
  protected page = 1;
  protected totalPages = 1;
  protected totalCount = 0;
  protected pageSize = 25;
  protected errorMessage = '';
  protected isLoading = true;

  ngOnInit(): void {
    this.loadPage(1);
  }

  protected onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.loadPage(1);
  }

  protected loadPage(page: number): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.auditLogApiService.getGlobalAuditLog(page, this.pageSize).subscribe({
      next: (result) => {
        this.entries = result.items;
        this.page = result.page;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.message ?? 'Unable to load audit log entries.';
        this.isLoading = false;
      }
    });
  }
}
