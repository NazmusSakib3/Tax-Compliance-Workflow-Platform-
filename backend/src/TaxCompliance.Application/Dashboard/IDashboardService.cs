namespace TaxCompliance.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    Task<byte[]> ExportComplianceReportAsync(CancellationToken cancellationToken);
}

