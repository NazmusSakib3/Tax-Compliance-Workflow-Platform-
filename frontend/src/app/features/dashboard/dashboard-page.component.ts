import { NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SummaryCardComponent, SummaryCardTone } from '../../shared/components/summary-card.component';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { DashboardBreakdownItem, DashboardSummary } from '../../core/models/dashboard.models';
import { AuthService } from '../../core/services/auth.service';
import { DashboardApiService } from '../../core/services/dashboard-api.service';
import { isContributorOnly } from '../../core/utils/role.utils';

interface SummaryCardConfig {
  label: string;
  value: number;
  link?: string;
  linkLabel?: string;
  queryParams?: Record<string, string>;
  tone?: SummaryCardTone;
}

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [NgFor, NgIf, RouterLink, SummaryCardComponent, LoadingStateComponent, EmptyStateComponent],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.css'
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboardApiService = inject(DashboardApiService);
  private readonly authService = inject(AuthService);

  protected summary: DashboardSummary | null = null;
  protected summaryCards: SummaryCardConfig[] = [];
  protected isLoading = true;
  protected isExporting = false;
  protected errorMessage = '';

  ngOnInit(): void {
    this.loadSummary();
  }

  protected get pageTitle(): string {
    return this.isContributorView ? 'My compliance workload' : 'Compliance dashboard';
  }

  protected get pageSubtitle(): string {
    return this.isContributorView
      ? 'Start with overdue and in-progress assignments, then open a task to add notes or upload evidence.'
      : 'Track the workload that needs attention first, drill into tasks, and export a compliance status snapshot.';
  }

  protected get trendDelta(): number {
    if (!this.summary) {
      return 0;
    }

    return this.summary.completedLast30Days - this.summary.completedPrevious30Days;
  }

  protected get trendDeltaLabel(): string {
    const delta = this.trendDelta;
    if (delta === 0) {
      return 'No change';
    }

    return delta > 0 ? `+${delta} vs prior` : `${delta} vs prior`;
  }

  protected get trendLabel(): string {
    if (!this.summary) {
      return '';
    }

    const delta = this.summary.completedLast30Days - this.summary.completedPrevious30Days;
    if (delta === 0) {
      return 'Completion pace is steady versus the previous 30 days.';
    }

    const direction = delta > 0 ? 'up' : 'down';
    return `Completions are ${direction} ${Math.abs(delta)} versus the previous 30-day window.`;
  }

  protected get isContributorView(): boolean {
    return isContributorOnly(this.authService);
  }

  protected get trendBarMax(): number {
    if (!this.summary) {
      return 0;
    }

    return Math.max(this.summary.completedLast30Days, this.summary.completedPrevious30Days, 1);
  }

  protected trendBarWidth(value: number): number {
    if (!this.trendBarMax) {
      return 0;
    }

    return Math.round((value / this.trendBarMax) * 100);
  }

  protected breakdownQueryParams(item: DashboardBreakdownItem): Record<string, string> {
    const params: Record<string, string> = { search: item.name };
    if (item.overdueCount > 0) {
      params['status'] = 'Overdue';
    }

    return params;
  }

  protected exportReport(): void {
    this.isExporting = true;
    this.dashboardApiService.exportComplianceReport().subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `compliance-status-${new Date().toISOString().slice(0, 10)}.csv`;
        anchor.click();
        URL.revokeObjectURL(url);
        this.isExporting = false;
      },
      error: () => {
        this.errorMessage = 'Unable to export the compliance report right now.';
        this.isExporting = false;
      }
    });
  }

  private loadSummary(): void {
    this.dashboardApiService.getSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.summaryCards = this.buildSummaryCards(summary);
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.message ?? 'Unable to load the dashboard summary right now.';
        this.isLoading = false;
      }
    });
  }

  private buildSummaryCards(summary: DashboardSummary): SummaryCardConfig[] {
    if (this.isContributorView) {
      return [
        {
          label: 'Assigned to me',
          value: summary.assignedToMeCount,
          link: '/task-occurrences',
          linkLabel: 'Open my tasks',
          queryParams: { assignedOnly: 'true' },
          tone: 'primary'
        },
        {
          label: 'Overdue tasks',
          value: summary.overdueCount,
          link: '/task-occurrences',
          linkLabel: 'Review overdue work',
          queryParams: { status: 'Overdue' },
          tone: 'danger'
        },
        {
          label: 'Due soon',
          value: summary.dueSoonCount,
          link: '/task-occurrences',
          linkLabel: 'See due soon tasks',
          tone: 'warning'
        },
        {
          label: 'In progress',
          value: summary.inProgressCount,
          link: '/task-occurrences',
          linkLabel: 'Continue in-progress work',
          queryParams: { status: 'InProgress' },
          tone: 'primary'
        }
      ];
    }

    return [
      {
        label: 'Overdue tasks',
        value: summary.overdueCount,
        link: '/task-occurrences',
        linkLabel: 'Review overdue work',
        queryParams: { status: 'Overdue' },
        tone: 'danger'
      },
      {
        label: 'Due soon',
        value: summary.dueSoonCount,
        link: '/task-occurrences',
        linkLabel: 'See due soon tasks',
        tone: 'warning'
      },
      {
        label: 'Completed',
        value: summary.completedCount,
        link: '/task-occurrences',
        linkLabel: 'View completed tasks',
        queryParams: { status: 'Completed' },
        tone: 'success'
      },
      {
        label: 'In progress',
        value: summary.inProgressCount,
        link: '/task-occurrences',
        linkLabel: 'Continue in-progress work',
        queryParams: { status: 'InProgress' },
        tone: 'primary'
      }
    ];
  }
}
