using Microsoft.EntityFrameworkCore;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskRules;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class ComplianceTaskRuleService : IComplianceTaskRuleService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IOrganizationScopeService organizationScope;
    private readonly ICurrentUserContextService currentUserContextService;

    public ComplianceTaskRuleService(
        ApplicationDbContext dbContext,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService)
    {
        this.dbContext = dbContext;
        this.organizationScope = organizationScope;
        this.currentUserContextService = currentUserContextService;
    }

    public async Task<PagedResult<ComplianceTaskRuleListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationHelper.Normalize(query.Page, query.PageSize);
        var ruleQuery = dbContext.ComplianceTaskRules
            .AsNoTracking()
            .Include(entity => entity.LegalEntity)
            .AsQueryable();

        ruleQuery = ruleQuery.ApplyOrganizationScope(
            organizationScope,
            currentUserContextService,
            entity => entity.LegalEntity!.OrganizationId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            ruleQuery = ruleQuery.Where(entity => entity.Title.Contains(search));
        }

        var totalCount = await ruleQuery.CountAsync(cancellationToken);
        var items = await ruleQuery
            .Include(entity => entity.Jurisdiction)
            .Include(entity => entity.ComplianceTemplate)
            .OrderBy(entity => entity.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new ComplianceTaskRuleListItemDto
            {
                Id = entity.Id,
                Title = entity.Title,
                LegalEntityName = entity.LegalEntity!.Name,
                JurisdictionName = entity.Jurisdiction!.Name,
                TemplateName = entity.ComplianceTemplate!.Name,
                RecurrenceType = entity.RecurrenceType,
                DueDayOfMonth = entity.DueDayOfMonth,
                DueMonthOfYear = entity.DueMonthOfYear,
                IsActive = entity.IsActive
            })
            .ToListAsync(cancellationToken);

        return PaginationHelper.Create(items, page, pageSize, totalCount);
    }

    public async Task<ComplianceTaskRuleDetailDto> GetByIdAsync(Guid complianceTaskRuleId, CancellationToken cancellationToken)
    {
        var entity = await LoadEntityAsync(complianceTaskRuleId, cancellationToken);
        EnsureEntityAccess(entity);
        return MapDetail(entity);
    }

    public async Task<ComplianceTaskRuleDetailDto> CreateAsync(SaveComplianceTaskRuleRequest request, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(request, cancellationToken);
        ValidateRecurrenceFields(request);

        var entity = new ComplianceTaskRule
        {
            LegalEntityId = request.LegalEntityId,
            JurisdictionId = request.JurisdictionId,
            ComplianceTemplateId = request.ComplianceTemplateId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            RecurrenceType = request.RecurrenceType,
            DueDayOfMonth = request.DueDayOfMonth,
            DueMonthOfYear = request.DueMonthOfYear,
            IsActive = request.IsActive
        };

        dbContext.ComplianceTaskRules.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<ComplianceTaskRuleDetailDto> UpdateAsync(Guid complianceTaskRuleId, SaveComplianceTaskRuleRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ComplianceTaskRules
            .Include(rule => rule.LegalEntity)
            .SingleOrDefaultAsync(item => item.Id == complianceTaskRuleId, cancellationToken)
            ?? throw new EntityNotFoundException("Compliance task rule was not found.");

        EnsureEntityAccess(entity);
        await ValidateReferencesAsync(request, cancellationToken);
        ValidateRecurrenceFields(request);

        entity.LegalEntityId = request.LegalEntityId;
        entity.JurisdictionId = request.JurisdictionId;
        entity.ComplianceTemplateId = request.ComplianceTemplateId;
        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.RecurrenceType = request.RecurrenceType;
        entity.DueDayOfMonth = request.DueDayOfMonth;
        entity.DueMonthOfYear = request.DueMonthOfYear;
        entity.IsActive = request.IsActive;
        entity.UpdatedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid complianceTaskRuleId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ComplianceTaskRules
            .Include(rule => rule.LegalEntity)
            .SingleOrDefaultAsync(item => item.Id == complianceTaskRuleId, cancellationToken)
            ?? throw new EntityNotFoundException("Compliance task rule was not found.");

        EnsureEntityAccess(entity);
        dbContext.ComplianceTaskRules.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EnsureEntityAccess(ComplianceTaskRule entity)
    {
        if (OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser())
            && !organizationScope.HasOrganizationScope())
        {
            return;
        }

        organizationScope.EnsureSameOrganization(entity.LegalEntity!.OrganizationId);
    }

    private async Task<ComplianceTaskRule> LoadEntityAsync(Guid complianceTaskRuleId, CancellationToken cancellationToken)
    {
        return await dbContext.ComplianceTaskRules
            .Include(entity => entity.LegalEntity)
            .Include(entity => entity.Jurisdiction)
            .Include(entity => entity.ComplianceTemplate)
            .SingleOrDefaultAsync(entity => entity.Id == complianceTaskRuleId, cancellationToken)
            ?? throw new EntityNotFoundException("Compliance task rule was not found.");
    }

    private async Task ValidateReferencesAsync(SaveComplianceTaskRuleRequest request, CancellationToken cancellationToken)
    {
        var legalEntity = await dbContext.LegalEntities.SingleOrDefaultAsync(entity => entity.Id == request.LegalEntityId, cancellationToken);
        if (legalEntity is null)
        {
            throw new AppValidationException("Legal entity is required.", new Dictionary<string, string[]>
            {
                ["legalEntityId"] = ["The selected legal entity does not exist."]
            });
        }

        if (!OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser())
            || organizationScope.HasOrganizationScope())
        {
            organizationScope.EnsureSameOrganization(legalEntity.OrganizationId);
        }

        var jurisdiction = await dbContext.Jurisdictions.SingleOrDefaultAsync(entity => entity.Id == request.JurisdictionId, cancellationToken);
        if (jurisdiction is null)
        {
            throw new AppValidationException("Jurisdiction is required.", new Dictionary<string, string[]>
            {
                ["jurisdictionId"] = ["The selected jurisdiction does not exist."]
            });
        }

        if (!OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser())
            || organizationScope.HasOrganizationScope())
        {
            organizationScope.EnsureSameOrganization(jurisdiction.OrganizationId);
        }

        var template = await dbContext.ComplianceTemplates.SingleOrDefaultAsync(entity => entity.Id == request.ComplianceTemplateId, cancellationToken);
        if (template is null)
        {
            throw new AppValidationException("Compliance template is required.", new Dictionary<string, string[]>
            {
                ["complianceTemplateId"] = ["The selected compliance template does not exist."]
            });
        }

        if (!OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser())
            || organizationScope.HasOrganizationScope())
        {
            organizationScope.EnsureSameOrganization(template.OrganizationId);
        }
    }

    private static void ValidateRecurrenceFields(SaveComplianceTaskRuleRequest request)
    {
        if (request.RecurrenceType == RecurrenceType.Yearly && !request.DueMonthOfYear.HasValue)
        {
            throw new AppValidationException("Yearly rules must include a due month.", new Dictionary<string, string[]>
            {
                ["dueMonthOfYear"] = ["Select a due month for yearly recurring rules."]
            });
        }

        if (request.RecurrenceType != RecurrenceType.Yearly)
        {
            request.DueMonthOfYear = null;
        }
    }

    private static ComplianceTaskRuleDetailDto MapDetail(ComplianceTaskRule entity)
    {
        return new ComplianceTaskRuleDetailDto
        {
            Id = entity.Id,
            LegalEntityId = entity.LegalEntityId,
            JurisdictionId = entity.JurisdictionId,
            ComplianceTemplateId = entity.ComplianceTemplateId,
            Title = entity.Title,
            Description = entity.Description,
            LegalEntityName = entity.LegalEntity?.Name ?? string.Empty,
            JurisdictionName = entity.Jurisdiction?.Name ?? string.Empty,
            TemplateName = entity.ComplianceTemplate?.Name ?? string.Empty,
            RecurrenceType = entity.RecurrenceType,
            DueDayOfMonth = entity.DueDayOfMonth,
            DueMonthOfYear = entity.DueMonthOfYear,
            IsActive = entity.IsActive
        };
    }
}
