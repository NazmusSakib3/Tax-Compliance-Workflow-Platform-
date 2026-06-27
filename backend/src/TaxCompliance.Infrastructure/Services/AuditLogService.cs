using Microsoft.EntityFrameworkCore;
using TaxCompliance.Application.AuditLog;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IOrganizationScopeService organizationScope;
    private readonly ICurrentUserContextService currentUserContextService;

    public AuditLogService(
        ApplicationDbContext dbContext,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService)
    {
        this.dbContext = dbContext;
        this.organizationScope = organizationScope;
        this.currentUserContextService = currentUserContextService;
    }

    public async Task<PagedResult<GlobalAuditLogEntryDto>> GetGlobalAuditLogAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationHelper.Normalize(query.Page, query.PageSize);

        var auditQuery = dbContext.AuditLogEntries
            .AsNoTracking()
            .Include(entry => entry.ComplianceTaskOccurrence)!
                .ThenInclude(occurrence => occurrence!.ComplianceTaskRule)!
                    .ThenInclude(rule => rule!.LegalEntity)
            .Include(entry => entry.ComplianceTaskOccurrence)!
                .ThenInclude(occurrence => occurrence!.ComplianceTaskRule)
            .AsQueryable();

        auditQuery = auditQuery.ApplyOrganizationScope(
            organizationScope,
            currentUserContextService,
            entry => entry.ComplianceTaskOccurrence!.ComplianceTaskRule!.LegalEntity!.OrganizationId);

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            auditQuery = auditQuery.Where(entry => entry.ActionType == query.ActionType);
        }

        var totalCount = await auditQuery.CountAsync(cancellationToken);

        var items = await auditQuery
            .OrderByDescending(entry => entry.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entry => new GlobalAuditLogEntryDto
            {
                Id = entry.Id,
                ComplianceTaskOccurrenceId = entry.ComplianceTaskOccurrenceId,
                ActionType = entry.ActionType,
                Description = entry.Description,
                PerformedByUserId = entry.PerformedByUserId,
                PerformedByDisplayName = entry.PerformedByDisplayName,
                CreatedUtc = entry.CreatedUtc,
                RuleTitle = entry.ComplianceTaskOccurrence!.ComplianceTaskRule!.Title,
                LegalEntityName = entry.ComplianceTaskOccurrence.ComplianceTaskRule.LegalEntity!.Name
            })
            .ToListAsync(cancellationToken);

        return PaginationHelper.Create(items, page, pageSize, totalCount);
    }
}
