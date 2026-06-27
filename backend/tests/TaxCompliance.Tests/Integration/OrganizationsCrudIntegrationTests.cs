using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.LegalEntities;
using TaxCompliance.Application.Organizations;

namespace TaxCompliance.Tests.Integration;

public class OrganizationsCrudIntegrationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public OrganizationsCrudIntegrationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task OrganizationCrudFlow_ShouldCreateReadUpdateAndDeleteOrganization()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(client));

        var createResponse = await client.PostAsJsonAsync("/api/organizations", new SaveOrganizationRequest
        {
            Name = "Northwind Holdings",
            Code = "NWH",
            Description = "Primary test organization",
            IsActive = true
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdOrganization = await createResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>();
        createdOrganization.Should().NotBeNull();
        createdOrganization!.Name.Should().Be("Northwind Holdings");

        var listResponse = await client.GetAsync("/api/organizations");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var organizations = await listResponse.Content.ReadFromJsonAsync<PagedResult<OrganizationListItemDto>>();
        organizations!.Items.Should().Contain(item => item.Id == createdOrganization.Id && item.Code == "NWH");

        var updateResponse = await client.PutAsJsonAsync($"/api/organizations/{createdOrganization.Id}", new SaveOrganizationRequest
        {
            Name = "Northwind Compliance Group",
            Code = "NCG",
            Description = "Updated organization",
            IsActive = false
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedOrganization = await updateResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>();
        updatedOrganization.Should().NotBeNull();
        updatedOrganization!.Name.Should().Be("Northwind Compliance Group");
        updatedOrganization.Code.Should().Be("NCG");
        updatedOrganization.IsActive.Should().BeFalse();

        var deleteResponse = await client.DeleteAsync($"/api/organizations/{createdOrganization.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDeleteResponse = await client.GetAsync($"/api/organizations/{createdOrganization.Id}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LegalEntityCrudCoreFlow_ShouldCreateAndListEntityWithinOrganization()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(client));

        var organizationResponse = await client.PostAsJsonAsync("/api/organizations", new SaveOrganizationRequest
        {
            Name = "Contoso Group",
            Code = "CTSO",
            Description = "Organization for legal entity flow",
            IsActive = true
        });

        var organization = await organizationResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>();
        organization.Should().NotBeNull();

        var legalEntityResponse = await client.PostAsJsonAsync("/api/legal-entities", new SaveLegalEntityRequest
        {
            OrganizationId = organization!.Id,
            Name = "Contoso Germany GmbH",
            RegistrationNumber = "HRB-2026-100",
            TaxIdentifier = "DE-999-100",
            IsActive = true
        });

        legalEntityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var legalEntity = await legalEntityResponse.Content.ReadFromJsonAsync<LegalEntityDetailDto>();
        legalEntity.Should().NotBeNull();
        legalEntity!.OrganizationId.Should().Be(organization.Id);

        var listResponse = await client.GetAsync("/api/legal-entities");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var legalEntities = await listResponse.Content.ReadFromJsonAsync<PagedResult<LegalEntityListItemDto>>();
        legalEntities!.Items.Should().Contain(item =>
            item.OrganizationId == organization.Id &&
            item.Name == "Contoso Germany GmbH" &&
            item.RegistrationNumber == "HRB-2026-100");
    }

    private static async Task<string> GetAccessTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@taxplatform.local",
            Password = "Admin123!"
        });

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return payload?.Data?.AccessToken ?? string.Empty;
    }
}
