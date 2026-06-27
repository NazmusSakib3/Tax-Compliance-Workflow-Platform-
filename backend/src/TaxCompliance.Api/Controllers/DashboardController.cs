using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Dashboard;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Provides dashboard summary metrics for the task workload.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    /// <summary>
    /// Returns overdue, due soon, completed, and in-progress task counts.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetSummaryAsync(cancellationToken));
    }

    /// <summary>
    /// Exports the current compliance status summary as CSV.
    /// </summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportComplianceReport(CancellationToken cancellationToken)
    {
        var reportBytes = await dashboardService.ExportComplianceReportAsync(cancellationToken);
        return File(reportBytes, "text/csv", $"compliance-status-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
