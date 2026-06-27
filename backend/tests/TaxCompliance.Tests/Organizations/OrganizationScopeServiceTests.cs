using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TaxCompliance.Application.Auth;
using TaxCompliance.Infrastructure.Services;
using TaxCompliance.Tests.TestDoubles;

namespace TaxCompliance.Tests.Organizations;

public class OrganizationScopeServiceTests
{
    [Fact]
    public void GetOrganizationId_ShouldReturnNullForPlatformAdminWithoutHeaderOverride()
    {
        var service = CreateService(new CurrentUserContext
        {
            Roles = [RoleNames.Admin]
        });

        service.GetOrganizationId().Should().BeNull();
        service.HasOrganizationScope().Should().BeFalse();
    }

    [Fact]
    public void GetOrganizationId_ShouldUseHeaderOverrideForPlatformAdmin()
    {
        var organizationId = Guid.NewGuid();
        var service = CreateService(
            new CurrentUserContext { Roles = [RoleNames.Admin] },
            organizationId.ToString());

        service.GetOrganizationId().Should().Be(organizationId);
    }

    [Fact]
    public void EnsureSameOrganization_ShouldAllowPlatformAdminWithoutScopedContext()
    {
        var service = CreateService(new CurrentUserContext { Roles = [RoleNames.Admin] });

        var action = () => service.EnsureSameOrganization(Guid.NewGuid());

        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureSameOrganization_ShouldRejectMismatchedScopedOrganization()
    {
        var scopedOrganizationId = Guid.NewGuid();
        var service = CreateService(
            new CurrentUserContext { Roles = [RoleNames.Admin] },
            scopedOrganizationId.ToString());

        var action = () => service.EnsureSameOrganization(Guid.NewGuid());

        action.Should().Throw<TaxCompliance.Application.Common.EntityNotFoundException>();
    }

    private static OrganizationScopeService CreateService(CurrentUserContext currentUser, string? organizationHeader = null)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(organizationHeader))
        {
            httpContext.Request.Headers[OrganizationContextHeaders.OrganizationId] = organizationHeader;
        }

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserContextService = new FakeCurrentUserContextService { CurrentUser = currentUser };
        return new OrganizationScopeService(currentUserContextService, httpContextAccessor);
    }
}
