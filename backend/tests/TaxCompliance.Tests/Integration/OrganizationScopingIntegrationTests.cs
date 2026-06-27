using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Jurisdictions;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Tests.Integration;

public class OrganizationScopingIntegrationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public OrganizationScopingIntegrationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task NonAdminUser_ShouldOnlySeeJurisdictionsInTheirOrganization()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var organizationA = new Organization
        {
            Name = "Scope Org A",
            Code = "SCOPE-A",
            Description = "First scoped organization",
            IsActive = true
        };
        var organizationB = new Organization
        {
            Name = "Scope Org B",
            Code = "SCOPE-B",
            Description = "Second scoped organization",
            IsActive = true
        };

        dbContext.Organizations.AddRange(organizationA, organizationB);
        await dbContext.SaveChangesAsync();

        dbContext.Jurisdictions.AddRange(
            new Jurisdiction
            {
                OrganizationId = organizationA.Id,
                Name = "Org A Jurisdiction",
                CountryCode = "US",
                RegionCode = "TX",
                FilingAuthority = "Texas Comptroller"
            },
            new Jurisdiction
            {
                OrganizationId = organizationB.Id,
                Name = "Org B Jurisdiction",
                CountryCode = "US",
                RegionCode = "CA",
                FilingAuthority = "California FTB"
            });
        await dbContext.SaveChangesAsync();

        var viewer = new ApplicationUser
        {
            UserName = "viewer.scope@example.com",
            Email = "viewer.scope@example.com",
            DisplayName = "Scoped Viewer",
            EmailConfirmed = true,
            OrganizationId = organizationA.Id
        };
        await userManager.CreateAsync(viewer, "Viewer123!");
        await userManager.AddToRoleAsync(viewer, RoleNames.Viewer);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = viewer.Email,
            Password = "Viewer123!"
        });
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Data!.AccessToken);

        var listResponse = await client.GetAsync("/api/jurisdictions?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var jurisdictions = await listResponse.Content.ReadFromJsonAsync<PagedResult<JurisdictionListItemDto>>();
        jurisdictions.Should().NotBeNull();
        jurisdictions!.Items.Should().ContainSingle(item => item.Name == "Org A Jurisdiction");
        jurisdictions.Items.Should().NotContain(item => item.Name == "Org B Jurisdiction");
    }
}
