using Microsoft.EntityFrameworkCore;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTemplates;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class ComplianceTemplateService : IComplianceTemplateService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IOrganizationScopeService organizationScope;
    private readonly ICurrentUserContextService currentUserContextService;

    public ComplianceTemplateService(
        ApplicationDbContext dbContext,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService)
    {
        this.dbContext = dbContext;
        this.organizationScope = organizationScope;
        this.currentUserContextService = currentUserContextService;
    }

    public async Task<PagedResult<ComplianceTemplateListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationHelper.Normalize(query.Page, query.PageSize);
        var templateQuery = dbContext.ComplianceTemplates.AsNoTracking().AsQueryable();
        templateQuery = templateQuery.ApplyOrganizationScope(
            organizationScope,
            currentUserContextService,
            entity => entity.OrganizationId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            templateQuery = templateQuery.Where(entity =>
                entity.Name.Contains(search) || entity.FilingType.Contains(search));
        }

        var totalCount = await templateQuery.CountAsync(cancellationToken);
        var items = await templateQuery
            .OrderBy(entity => entity.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new ComplianceTemplateListItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                FilingType = entity.FilingType,
                ReminderDaysBeforeDue = entity.ReminderDaysBeforeDue,
                IsActive = entity.IsActive
            })
            .ToListAsync(cancellationToken);

        return PaginationHelper.Create(items, page, pageSize, totalCount);
    }

    public async Task<ComplianceTemplateDetailDto> GetByIdAsync(Guid complianceTemplateId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ComplianceTemplates.SingleOrDefaultAsync(item => item.Id == complianceTemplateId, cancellationToken)
            ?? throw new EntityNotFoundException("Compliance template was not found.");

        EnsureEntityAccess(entity);
        return MapDetail(entity);
    }

    public async Task<ComplianceTemplateDetailDto> CreateAsync(SaveComplianceTemplateRequest request, CancellationToken cancellationToken)
    {
        var organizationId = organizationScope.RequireOrganizationId();
        await EnsureNameIsUniqueAsync(request.Name, organizationId, null, cancellationToken);

        var entity = new ComplianceTemplate
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            FilingType = request.FilingType.Trim(),
            Description = request.Description.Trim(),
            ReminderDaysBeforeDue = request.ReminderDaysBeforeDue,
            IsActive = request.IsActive
        };

        dbContext.ComplianceTemplates.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task<ComplianceTemplateDetailDto> UpdateAsync(Guid complianceTemplateId, SaveComplianceTemplateRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ComplianceTemplates.SingleOrDefaultAsync(item => item.Id == complianceTemplateId, cancellationToken)
            ?? throw new EntityNotFoundException("Compliance template was not found.");

        EnsureEntityAccess(entity);
        await EnsureNameIsUniqueAsync(request.Name, entity.OrganizationId, complianceTemplateId, cancellationToken);

        entity.Name = request.Name.Trim();
        entity.FilingType = request.FilingType.Trim();
        entity.Description = request.Description.Trim();
        entity.ReminderDaysBeforeDue = request.ReminderDaysBeforeDue;
        entity.IsActive = request.IsActive;
        entity.UpdatedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task DeleteAsync(Guid complianceTemplateId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ComplianceTemplates
            .Include(item => item.ComplianceTaskRules)
            .SingleOrDefaultAsync(item => item.Id == complianceTemplateId, cancellationToken)
            ?? throw new EntityNotFoundException("Compliance template was not found.");

        EnsureEntityAccess(entity);

        if (entity.ComplianceTaskRules.Count > 0)
        {
            throw new AppValidationException("Compliance templates with task rules cannot be deleted.");
        }

        dbContext.ComplianceTemplates.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EnsureEntityAccess(ComplianceTemplate entity)
    {
        if (OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser())
            && !organizationScope.HasOrganizationScope())
        {
            return;
        }

        organizationScope.EnsureSameOrganization(entity.OrganizationId);
    }

    private async Task EnsureNameIsUniqueAsync(string name, Guid organizationId, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var exists = await dbContext.ComplianceTemplates.AnyAsync(
            item => item.OrganizationId == organizationId && item.Name == normalizedName && item.Id != currentId,
            cancellationToken);

        if (exists)
        {
            throw new AppValidationException("Template name must be unique.", new Dictionary<string, string[]>
            {
                ["name"] = ["Another compliance template already uses this name."]
            });
        }
    }

    private static ComplianceTemplateDetailDto MapDetail(ComplianceTemplate entity)
    {
        return new ComplianceTemplateDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            FilingType = entity.FilingType,
            Description = entity.Description,
            ReminderDaysBeforeDue = entity.ReminderDaysBeforeDue,
            IsActive = entity.IsActive
        };
    }
}
