using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Organizations;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IOrganizationScopeService organizationScope;
    private readonly ICurrentUserContextService currentUserContextService;
    private readonly UserManager<ApplicationUser> userManager;

    public OrganizationService(
        ApplicationDbContext dbContext,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService,
        UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.organizationScope = organizationScope;
        this.currentUserContextService = currentUserContextService;
        this.userManager = userManager;
    }

    public async Task<PagedResult<OrganizationListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationHelper.Normalize(query.Page, query.PageSize);
        var organizationQuery = dbContext.Organizations.AsNoTracking().AsQueryable();

        if (organizationScope.GetOrganizationId() is Guid organizationId)
        {
            organizationQuery = organizationQuery.Where(entity => entity.Id == organizationId);
        }
        else if (!OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser()))
        {
            organizationScope.RequireOrganizationId();
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            organizationQuery = organizationQuery.Where(entity =>
                entity.Name.Contains(search) || entity.Code.Contains(search));
        }

        var totalCount = await organizationQuery.CountAsync(cancellationToken);
        var items = await organizationQuery
            .OrderBy(entity => entity.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new OrganizationListItemDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = entity.Code,
                IsActive = entity.IsActive,
                LegalEntityCount = entity.LegalEntities.Count
            })
            .ToListAsync(cancellationToken);

        return PaginationHelper.Create(items, page, pageSize, totalCount);
    }

    public async Task<OrganizationDetailDto> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        EnsureOrganizationAccess(organizationId);

        var entity = await dbContext.Organizations
            .SingleOrDefaultAsync(organization => organization.Id == organizationId, cancellationToken);

        return entity is null
            ? throw new EntityNotFoundException("Organization was not found.")
            : MapDetail(entity);
    }

    public async Task<OrganizationDetailDto> CreateAsync(SaveOrganizationRequest request, CancellationToken cancellationToken)
    {
        var currentUser = currentUserContextService.GetCurrentUser();
        if (!OrganizationQueryExtensions.IsPlatformAdmin(currentUser) && organizationScope.HasOrganizationScope())
        {
            throw new AppValidationException("Your account is already assigned to an organization.");
        }

        await EnsureCodeIsUniqueAsync(request.Code, null, cancellationToken);

        var entity = new Organization
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            Description = request.Description.Trim(),
            IsActive = request.IsActive
        };

        dbContext.Organizations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!OrganizationQueryExtensions.IsPlatformAdmin(currentUser) &&
            !string.IsNullOrWhiteSpace(currentUser.UserId) &&
            !organizationScope.HasOrganizationScope())
        {
            var user = await userManager.FindByIdAsync(currentUser.UserId);
            if (user is not null)
            {
                user.OrganizationId = entity.Id;
                await userManager.UpdateAsync(user);
            }
        }

        return MapDetail(entity);
    }

    public async Task<OrganizationDetailDto> UpdateAsync(Guid organizationId, SaveOrganizationRequest request, CancellationToken cancellationToken)
    {
        EnsureOrganizationAccess(organizationId);

        var entity = await dbContext.Organizations
            .SingleOrDefaultAsync(organization => organization.Id == organizationId, cancellationToken)
            ?? throw new EntityNotFoundException("Organization was not found.");

        await EnsureCodeIsUniqueAsync(request.Code, organizationId, cancellationToken);

        entity.Name = request.Name.Trim();
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Description = request.Description.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapDetail(entity);
    }

    public async Task DeleteAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        EnsureOrganizationAccess(organizationId);

        var entity = await dbContext.Organizations
            .Include(organization => organization.LegalEntities)
            .SingleOrDefaultAsync(organization => organization.Id == organizationId, cancellationToken)
            ?? throw new EntityNotFoundException("Organization was not found.");

        if (entity.LegalEntities.Count > 0)
        {
            throw new AppValidationException("Organizations with legal entities cannot be deleted.");
        }

        dbContext.Organizations.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EnsureOrganizationAccess(Guid organizationId)
    {
        organizationScope.EnsureSameOrganization(organizationId);
    }

    private async Task EnsureCodeIsUniqueAsync(string code, Guid? currentOrganizationId, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var exists = await dbContext.Organizations.AnyAsync(
            organization => organization.Code == normalizedCode && organization.Id != currentOrganizationId,
            cancellationToken);

        if (exists)
        {
            throw new AppValidationException("Organization code must be unique.", new Dictionary<string, string[]>
            {
                ["code"] = ["An organization with this code already exists."]
            });
        }
    }

    private static OrganizationDetailDto MapDetail(Organization entity)
    {
        return new OrganizationDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }
}
