using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TaxCompliance.Application.AuditLog;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Users;

namespace TaxCompliance.Tests.Integration;

public class UsersAndAuditLogIntegrationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public UsersAndAuditLogIntegrationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task UsersEndpoint_ShouldAllowAdminToCreateAndListUsers()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(client));

        var createResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            Email = "viewer.user@example.com",
            DisplayName = "Viewer User",
            Password = "Viewer123!",
            Role = RoleNames.Viewer
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/users");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await listResponse.Content.ReadFromJsonAsync<PagedResult<UserListItemDto>>();
        users.Should().NotBeNull();
        users!.Items.Should().Contain(user => user.Email == "viewer.user@example.com" && user.Roles.Contains(RoleNames.Viewer));
    }

    [Fact]
    public async Task AuditLogEndpoint_ShouldReturnPagedResults()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(client));

        var response = await client.GetAsync("/api/audit-log?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<PagedResult<GlobalAuditLogEntryDto>>();
        payload.Should().NotBeNull();
        payload!.Page.Should().Be(1);
        payload.PageSize.Should().Be(10);
        payload.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
