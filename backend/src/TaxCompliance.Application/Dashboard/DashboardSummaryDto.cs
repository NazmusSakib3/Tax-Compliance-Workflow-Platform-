namespace TaxCompliance.Application.Dashboard;

public class DashboardSummaryDto
{
    public int OverdueCount { get; set; }
    public int DueSoonCount { get; set; }
    public int CompletedCount { get; set; }
    public int InProgressCount { get; set; }
    public int AssignedToMeCount { get; set; }
    public int CompletedLast30Days { get; set; }
    public int CompletedPrevious30Days { get; set; }
    public IReadOnlyCollection<DashboardBreakdownItemDto> JurisdictionBreakdown { get; set; } = Array.Empty<DashboardBreakdownItemDto>();
    public IReadOnlyCollection<DashboardBreakdownItemDto> LegalEntityBreakdown { get; set; } = Array.Empty<DashboardBreakdownItemDto>();
}

