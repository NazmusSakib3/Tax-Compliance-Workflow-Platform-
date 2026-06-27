using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaxCompliance.Application.Auth;

namespace TaxCompliance.Infrastructure.Services;

public class CurrentUserContextService : ICurrentUserContextService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserContextService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public CurrentUserContext GetCurrentUser()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
        {
            return new CurrentUserContext();
        }

        Guid? organizationId = null;
        var organizationClaim = principal.FindFirstValue(AuthClaimTypes.OrganizationId);
        if (Guid.TryParse(organizationClaim, out var parsedOrganizationId))
        {
            organizationId = parsedOrganizationId;
        }

        return new CurrentUserContext
        {
            UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            DisplayName = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            OrganizationId = organizationId,
            Roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        };
    }
}
