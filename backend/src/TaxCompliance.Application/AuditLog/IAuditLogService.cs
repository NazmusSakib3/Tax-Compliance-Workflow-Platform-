using TaxCompliance.Application.Common;

namespace TaxCompliance.Application.AuditLog;

public interface IAuditLogService
{
    Task<PagedResult<GlobalAuditLogEntryDto>> GetGlobalAuditLogAsync(AuditLogQuery query, CancellationToken cancellationToken);
}
