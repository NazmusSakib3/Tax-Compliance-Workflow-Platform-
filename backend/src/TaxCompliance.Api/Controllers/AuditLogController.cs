using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.AuditLog;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Provides a global, paginated audit log across all task occurrences.
/// </summary>
[ApiController]
[Route("api/audit-log")]
[Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        this.auditLogService = auditLogService;
    }

    /// <summary>
    /// Returns paginated audit log entries across all task occurrences.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GlobalAuditLogEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<GlobalAuditLogEntryDto>>> GetGlobalAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await auditLogService.GetGlobalAuditLogAsync(new AuditLogQuery
        {
            Page = page,
            PageSize = pageSize,
            ActionType = actionType
        }, cancellationToken);

        return Ok(result);
    }
}
