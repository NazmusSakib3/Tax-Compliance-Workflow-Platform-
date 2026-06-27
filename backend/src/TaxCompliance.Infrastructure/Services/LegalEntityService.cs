using Microsoft.EntityFrameworkCore;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.LegalEntities;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class LegalEntityService : ILegalEntityService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IOrganizationScopeService organizationScope;
    private readonly ICurrentUserContextService currentUserContextService;

    public LegalEntityService(
        ApplicationDbContext dbContext,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService)
    {
        this.dbContext = dbContext;
        this.organizationScope = organizationScope;
        this.currentUserContextService = currentUserContextService;
    }

    public async Task<PagedResult<LegalEntityListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationHelper.Normalize(query.Page, query.PageSize);
        var legalEntityQuery = dbContext.LegalEntities.AsNoTracking().AsQueryable();
        legalEntityQuery = legalEntityQuery.ApplyOrganizationScope(
            organizationScope,
            currentUserContextService,
            entity => entity.OrganizationId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            legalEntityQuery = legalEntityQuery.Where(entity =>
                entity.Name.Contains(search) ||
                entity.RegistrationNumber.Contains(search) ||
                entity.TaxIdentifier.Contains(search));
        }

        var totalCount = await legalEntityQuery.CountAsync(cancellationToken);
        var items = await legalEntityQuery
            .Include(entity => entity.Organization)
            .OrderBy(entity => entity.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new LegalEntityListItemDto
            {
                Id = entity.Id,
                OrganizationId = entity.OrganizationId,
                OrganizationName = entity.Organization!.Name,
                Name = entity.Name,
                RegistrationNumber = entity.RegistrationNumber,
                TaxIdentifier = entity.TaxIdentifier,
                IsActive = entity.IsActive
            })
            .ToListAsync(cancellationToken);

        return PaginationHelper.Create(items, page, pageSize, totalCount);
    }

    public async Task<LegalEntityDetailDto> GetByIdAsync(Guid legalEntityId, CancellationToken cancellationToken)
    {
        var entity = await LoadEntityAsync(legalEntityId, cancellationToken);
        EnsureEntityAccess(entity);
        return MapDetail(entity);
    }

    public async Task<LegalEntityDetailDto> CreateAsync(SaveLegalEntityRequest request, CancellationToken cancellationToken)
    {
        var organizationId = ResolveOrganizationId(request.OrganizationId);
        await EnsureOrganizationExistsAsync(organizationId, cancellationToken);
        await EnsureUniqueIdentifiersAsync(request.RegistrationNumber, request.TaxIdentifier, null, cancellationToken);

        var entity = new LegalEntity
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            RegistrationNumber = request.RegistrationNumber.Trim(),
            TaxIdentifier = request.TaxIdentifier.Trim(),
            IsActive = request.IsActive
        };

        dbContext.LegalEntities.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<LegalEntityDetailDto> UpdateAsync(Guid legalEntityId, SaveLegalEntityRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.LegalEntities.SingleOrDefaultAsync(item => item.Id == legalEntityId, cancellationToken)
            ?? throw new EntityNotFoundException("Legal entity was not found.");

        EnsureEntityAccess(entity);

        var organizationId = ResolveOrganizationId(request.OrganizationId);
        await EnsureOrganizationExistsAsync(organizationId, cancellationToken);
        await EnsureUniqueIdentifiersAsync(request.RegistrationNumber, request.TaxIdentifier, legalEntityId, cancellationToken);

        entity.OrganizationId = organizationId;
        entity.Name = request.Name.Trim();
        entity.RegistrationNumber = request.RegistrationNumber.Trim();
        entity.TaxIdentifier = request.TaxIdentifier.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid legalEntityId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.LegalEntities
            .Include(item => item.ComplianceTaskRules)
            .SingleOrDefaultAsync(item => item.Id == legalEntityId, cancellationToken)
            ?? throw new EntityNotFoundException("Legal entity was not found.");

        EnsureEntityAccess(entity);

        if (entity.ComplianceTaskRules.Count > 0)
        {
            throw new AppValidationException("Legal entities with task rules cannot be deleted.");
        }

        dbContext.LegalEntities.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid ResolveOrganizationId(Guid requestedOrganizationId)
    {
        if (organizationScope.HasOrganizationScope())
        {
            return organizationScope.RequireOrganizationId();
        }

        if (OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser()))
        {
            return requestedOrganizationId;
        }

        return organizationScope.RequireOrganizationId();
    }

    private void EnsureEntityAccess(LegalEntity entity)
    {
        if (OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser())
            && !organizationScope.HasOrganizationScope())
        {
            return;
        }

        organizationScope.EnsureSameOrganization(entity.OrganizationId);
    }

    private async Task<LegalEntity> LoadEntityAsync(Guid legalEntityId, CancellationToken cancellationToken)
    {
        return await dbContext.LegalEntities
            .Include(entity => entity.Organization)
            .SingleOrDefaultAsync(entity => entity.Id == legalEntityId, cancellationToken)
            ?? throw new EntityNotFoundException("Legal entity was not found.");
    }

    private async Task EnsureOrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Organizations.AnyAsync(entity => entity.Id == organizationId, cancellationToken);
        if (!exists)
        {
            throw new AppValidationException("Organization is required.", new Dictionary<string, string[]>
            {
                ["organizationId"] = ["The selected organization does not exist."]
            });
        }
    }

    private async Task EnsureUniqueIdentifiersAsync(string registrationNumber, string taxIdentifier, Guid? currentId, CancellationToken cancellationToken)
    {
        var registrationExists = await dbContext.LegalEntities.AnyAsync(
            entity => entity.RegistrationNumber == registrationNumber.Trim() && entity.Id != currentId,
            cancellationToken);

        if (registrationExists)
        {
            throw new AppValidationException("Registration number must be unique.", new Dictionary<string, string[]>
            {
                ["registrationNumber"] = ["Another legal entity already uses this registration number."]
            });
        }

        var taxIdentifierExists = await dbContext.LegalEntities.AnyAsync(
            entity => entity.TaxIdentifier == taxIdentifier.Trim() && entity.Id != currentId,
            cancellationToken);

        if (taxIdentifierExists)
        {
            throw new AppValidationException("Tax identifier must be unique.", new Dictionary<string, string[]>
            {
                ["taxIdentifier"] = ["Another legal entity already uses this tax identifier."]
            });
        }
    }

    private static LegalEntityDetailDto MapDetail(LegalEntity entity)
    {
        return new LegalEntityDetailDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            OrganizationName = entity.Organization?.Name ?? string.Empty,
            Name = entity.Name,
            RegistrationNumber = entity.RegistrationNumber,
            TaxIdentifier = entity.TaxIdentifier,
            IsActive = entity.IsActive
        };
    }
}
