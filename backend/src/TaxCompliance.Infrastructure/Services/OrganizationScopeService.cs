using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using Microsoft.AspNetCore.Http;

namespace TaxCompliance.Infrastructure.Services;

public class OrganizationScopeService : IOrganizationScopeService
{
    private readonly ICurrentUserContextService currentUserContextService;
    private readonly IHttpContextAccessor httpContextAccessor;

    public OrganizationScopeService(
        ICurrentUserContextService currentUserContextService,
        IHttpContextAccessor httpContextAccessor)
    {
        this.currentUserContextService = currentUserContextService;
        this.httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetOrganizationId()
    {
        var currentUser = currentUserContextService.GetCurrentUser();
        if (OrganizationQueryExtensions.IsPlatformAdmin(currentUser))
        {
            var headerValue = httpContextAccessor.HttpContext?.Request.Headers[OrganizationContextHeaders.OrganizationId]
                .FirstOrDefault();

            if (Guid.TryParse(headerValue, out var headerOrganizationId))
            {
                return headerOrganizationId;
            }

            return null;
        }

        return currentUser.OrganizationId;
    }

    public Guid RequireOrganizationId()
    {
        var organizationId = GetOrganizationId();
        if (!organizationId.HasValue)
        {
            throw new AppValidationException("Your account is not assigned to an organization.");
        }

        return organizationId.Value;
    }

    public bool HasOrganizationScope() => GetOrganizationId().HasValue;

    public void EnsureSameOrganization(Guid organizationId)
    {
        var currentOrganizationId = GetOrganizationId();
        if (!currentOrganizationId.HasValue)
        {
            if (OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser()))
            {
                return;
            }

            throw new EntityNotFoundException("The requested resource was not found.");
        }

        if (currentOrganizationId.Value != organizationId)
        {
            throw new EntityNotFoundException("The requested resource was not found.");
        }
    }
}
