export interface DashboardBreakdownItem {
  name: string;
  openCount: number;
  overdueCount: number;
}

export interface DashboardSummary {
  overdueCount: number;
  dueSoonCount: number;
  completedCount: number;
  inProgressCount: number;
  assignedToMeCount: number;
  completedLast30Days: number;
  completedPrevious30Days: number;
  jurisdictionBreakdown: DashboardBreakdownItem[];
  legalEntityBreakdown: DashboardBreakdownItem[];
}
