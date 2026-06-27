using Microsoft.EntityFrameworkCore;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Jurisdictions;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class JurisdictionService : IJurisdictionService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IOrganizationScopeService organizationScope;
    private readonly ICurrentUserContextService currentUserContextService;

    public JurisdictionService(
        ApplicationDbContext dbContext,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService)
    {
        this.dbContext = dbContext;
        this.organizationScope = organizationScope;
        this.currentUserContextService = currentUserContextService;
    }

    public async Task<PagedResult<JurisdictionListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationHelper.Normalize(query.Page, query.PageSize);
        var jurisdictionQuery = dbContext.Jurisdictions.AsNoTracking().AsQueryable();
        jurisdictionQuery = jurisdictionQuery.ApplyOrganizationScope(
            organizationScope,
            currentUserContextService,
            entity => entity.OrganizationId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            jurisdictionQuery = jurisdictionQuery.Where(entity =>
                entity.Name.Contains(search) ||
                entity.CountryCode.Contains(search) ||
                entity.RegionCode.Contains(search));
        }

        var totalCount = await jurisdictionQuery.CountAsync(cancellationToken);
        var items = await jurisdictionQuery
            .OrderBy(entity => entity.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new JurisdictionListItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                CountryCode = entity.CountryCode,
                RegionCode = entity.RegionCode,
                FilingAuthority = entity.FilingAuthority,
                IsActive = entity.IsActive
            })
            .ToListAsync(cancellationToken);

        return PaginationHelper.Create(items, page, pageSize, totalCount);
    }

    public async Task<JurisdictionDetailDto> GetByIdAsync(Guid jurisdictionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Jurisdictions.SingleOrDefaultAsync(item => item.Id == jurisdictionId, cancellationToken)
            ?? throw new EntityNotFoundException("Jurisdiction was not found.");

        EnsureEntityAccess(entity);
        return MapDetail(entity);
    }

    public async Task<JurisdictionDetailDto> CreateAsync(SaveJurisdictionRequest request, CancellationToken cancellationToken)
    {
        var organizationId = organizationScope.RequireOrganizationId();
        await EnsureUniqueRegionAsync(request.CountryCode, request.RegionCode, organizationId, null, cancellationToken);

        var entity = new Jurisdiction
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            RegionCode = request.RegionCode.Trim().ToUpperInvariant(),
            FilingAuthority = request.FilingAuthority.Trim(),
            IsActive = request.IsActive
        };

        dbContext.Jurisdictions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task<JurisdictionDetailDto> UpdateAsync(Guid jurisdictionId, SaveJurisdictionRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Jurisdictions.SingleOrDefaultAsync(item => item.Id == jurisdictionId, cancellationToken)
            ?? throw new EntityNotFoundException("Jurisdiction was not found.");

        EnsureEntityAccess(entity);
        await EnsureUniqueRegionAsync(request.CountryCode, request.RegionCode, entity.OrganizationId, jurisdictionId, cancellationToken);

        entity.Name = request.Name.Trim();
        entity.CountryCode = request.CountryCode.Trim().ToUpperInvariant();
        entity.RegionCode = request.RegionCode.Trim().ToUpperInvariant();
        entity.FilingAuthority = request.FilingAuthority.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task DeleteAsync(Guid jurisdictionId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Jurisdictions
            .Include(item => item.ComplianceTaskRules)
            .SingleOrDefaultAsync(item => item.Id == jurisdictionId, cancellationToken)
            ?? throw new EntityNotFoundException("Jurisdiction was not found.");

        EnsureEntityAccess(entity);

        if (entity.ComplianceTaskRules.Count > 0)
        {
            throw new AppValidationException("Jurisdictions with task rules cannot be deleted.");
        }

        dbContext.Jurisdictions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EnsureEntityAccess(Jurisdiction entity)
    {
        if (OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser())
            && !organizationScope.HasOrganizationScope())
        {
            return;
        }

        organizationScope.EnsureSameOrganization(entity.OrganizationId);
    }

    private async Task EnsureUniqueRegionAsync(
        string countryCode,
        string regionCode,
        Guid organizationId,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();
        var normalizedRegionCode = regionCode.Trim().ToUpperInvariant();

        var exists = await dbContext.Jurisdictions.AnyAsync(
            entity => entity.OrganizationId == organizationId &&
                      entity.CountryCode == normalizedCountryCode &&
                      entity.RegionCode == normalizedRegionCode &&
                      entity.Id != currentId,
            cancellationToken);

        if (exists)
        {
            throw new AppValidationException("Jurisdiction code must be unique.", new Dictionary<string, string[]>
            {
                ["regionCode"] = ["Another jurisdiction already uses this country and region code."]
            });
        }
    }

    private static JurisdictionDetailDto MapDetail(Jurisdiction entity)
    {
        return new JurisdictionDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CountryCode = entity.CountryCode,
            RegionCode = entity.RegionCode,
            FilingAuthority = entity.FilingAuthority,
            IsActive = entity.IsActive
        };
    }
}
