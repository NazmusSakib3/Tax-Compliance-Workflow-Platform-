using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskOccurrences;
using TaxCompliance.Application.ComplianceTaskRules;
using TaxCompliance.Application.ComplianceTemplates;
using TaxCompliance.Application.Dashboard;
using TaxCompliance.Application.Jurisdictions;
using TaxCompliance.Application.LegalEntities;
using TaxCompliance.Application.Organizations;
using TaxCompliance.Domain.Enums;

namespace TaxCompliance.Tests.Integration;

public class RefreshTokenIntegrationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public RefreshTokenIntegrationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Refresh_ShouldRotateToken_AndRejectReusedRefreshToken()
    {
        using var client = factory.CreateClient();
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        await AuthIntegrationTestsHelper.CreateUserAsync(factory, client, email, "Refresh User");

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "ValidPass123!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        loginPayload!.Data!.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = loginPayload.Data.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        refreshPayload!.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshPayload.Data.RefreshToken.Should().NotBe(loginPayload.Data.RefreshToken);

        var reusedRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = loginPayload.Data.RefreshToken
        });
        reusedRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldRejectLockedOutUser()
    {
        using var client = factory.CreateClient();
        var email = $"refresh-lockout-{Guid.NewGuid():N}@example.com";
        await AuthIntegrationTestsHelper.CreateUserAsync(factory, client, email, "Locked Refresh User");

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "ValidPass123!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        loginPayload!.Data!.RefreshToken.Should().NotBeNullOrWhiteSpace();

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Infrastructure.Identity.ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user.Should().NotBeNull();
            await userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddYears(100));
        }

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = loginPayload.Data.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class DashboardExportIntegrationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public DashboardExportIntegrationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Export_ShouldReturnCsv_ForAuthenticatedAdmin()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await AuthIntegrationTestsHelper.GetAccessTokenAsync(client, "admin@taxplatform.local", "Admin123!"));

        var response = await client.GetAsync("/api/dashboard/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Metric,Value");
        body.Should().Contain("Overdue tasks");
        body.Should().Contain("Jurisdiction,Open tasks,Overdue tasks");
        body.Should().Contain("Legal entity,Open tasks,Overdue tasks");
    }

    [Fact]
    public async Task Export_ShouldIncludeBreakdownRows_AfterSeedingOccurrences()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await AuthIntegrationTestsHelper.GetAccessTokenAsync(client, "admin@taxplatform.local", "Admin123!"));

        await SeedDashboardDataAsync(client);

        var response = await client.GetAsync("/api/dashboard/export");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var csv = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        csv.Should().Contain("Export Jurisdiction");
        csv.Should().Contain("Export Legal Entity");
    }

    [Fact]
    public async Task GetSummary_ShouldIncludeBreakdownCounts_AfterSeedingOccurrences()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await AuthIntegrationTestsHelper.GetAccessTokenAsync(client, "admin@taxplatform.local", "Admin123!"));

        await SeedDashboardDataAsync(client);

        var response = await client.GetAsync("/api/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        summary.Should().NotBeNull();
        summary!.JurisdictionBreakdown.Should().Contain(item => item.Name == "Export Jurisdiction");
        summary.LegalEntityBreakdown.Should().Contain(item => item.Name == "Export Legal Entity");
    }

    private async Task SeedDashboardDataAsync(HttpClient client)
    {
        var organizationResponse = await client.PostAsJsonAsync("/api/organizations", new SaveOrganizationRequest
        {
            Name = "Export Test Org",
            Code = $"EXP-{Guid.NewGuid():N}"[..12],
            Description = "Dashboard export organization",
            IsActive = true
        });
        organizationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var organization = await organizationResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>();
        client.DefaultRequestHeaders.Remove(OrganizationContextHeaders.OrganizationId);
        client.DefaultRequestHeaders.Add(OrganizationContextHeaders.OrganizationId, organization!.Id.ToString());

        var jurisdictionResponse = await client.PostAsJsonAsync("/api/jurisdictions", new SaveJurisdictionRequest
        {
            Name = "Export Jurisdiction",
            CountryCode = "US",
            RegionCode = "NY",
            FilingAuthority = "NY DTF",
            IsActive = true
        });
        jurisdictionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var jurisdiction = await jurisdictionResponse.Content.ReadFromJsonAsync<JurisdictionDetailDto>();

        var templateResponse = await client.PostAsJsonAsync("/api/compliance-templates", new SaveComplianceTemplateRequest
        {
            Name = "Export VAT Template",
            FilingType = "VAT",
            Description = "Export template",
            ReminderDaysBeforeDue = 7,
            IsActive = true
        });
        templateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = await templateResponse.Content.ReadFromJsonAsync<ComplianceTemplateDetailDto>();

        var legalEntityResponse = await client.PostAsJsonAsync("/api/legal-entities", new SaveLegalEntityRequest
        {
            OrganizationId = organization.Id,
            Name = "Export Legal Entity",
            RegistrationNumber = $"EXP-{Guid.NewGuid():N}"[..16],
            TaxIdentifier = $"TAX-{Guid.NewGuid():N}"[..16],
            IsActive = true
        });
        legalEntityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var legalEntity = await legalEntityResponse.Content.ReadFromJsonAsync<LegalEntityDetailDto>();

        var ruleResponse = await client.PostAsJsonAsync("/api/compliance-task-rules", new SaveComplianceTaskRuleRequest
        {
            LegalEntityId = legalEntity!.Id,
            JurisdictionId = jurisdiction!.Id,
            ComplianceTemplateId = template!.Id,
            Title = "Export Monthly Rule",
            Description = "Generated for dashboard export test",
            RecurrenceType = RecurrenceType.Monthly,
            DueDayOfMonth = 15,
            IsActive = true
        });
        ruleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var generateResponse = await client.PostAsync("/api/compliance-task-occurrences/generate", null);
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class MfaLoginIntegrationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public MfaLoginIntegrationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Login_ShouldRequireMfaCode_AfterMfaIsEnabled()
    {
        using var client = factory.CreateClient();
        var email = $"mfa-login-{Guid.NewGuid():N}@example.com";
        await AuthIntegrationTestsHelper.CreateUserAsync(factory, client, email, "MFA Login User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await AuthIntegrationTestsHelper.GetAccessTokenAsync(client, email, "ValidPass123!"));

        var setupResponse = await client.PostAsync("/api/auth/mfa/setup", null);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var setupPayload = await setupResponse.Content.ReadFromJsonAsync<ApiResponse<MfaSetupResponse>>();
        var sharedKey = setupPayload!.Data!.SharedKey;
        var totp = new Totp(Base32Encoding.ToBytes(sharedKey));
        var enableCode = totp.ComputeTotp();

        var enableResponse = await client.PostAsJsonAsync("/api/auth/mfa/enable", new MfaVerifyRequest { Code = enableCode });
        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = null;
        var challengeResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "ValidPass123!"
        });
        challengeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var challengePayload = await challengeResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        challengePayload!.Data!.RequiresMfa.Should().BeTrue();

        var loginCode = totp.ComputeTotp();
        var mfaLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "ValidPass123!",
            MfaCode = loginCode
        });
        mfaLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginPayload = await mfaLoginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        loginPayload!.Success.Should().BeTrue();
        loginPayload.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task EnableMfa_ShouldRequireValidCode_AndDisableMfa_ShouldTurnOffProtection()
    {
        using var client = factory.CreateClient();
        var email = $"mfa-toggle-{Guid.NewGuid():N}@example.com";
        await AuthIntegrationTestsHelper.CreateUserAsync(factory, client, email, "MFA Toggle User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await AuthIntegrationTestsHelper.GetAccessTokenAsync(client, email, "ValidPass123!"));

        var setupResponse = await client.PostAsync("/api/auth/mfa/setup", null);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var setupPayload = await setupResponse.Content.ReadFromJsonAsync<ApiResponse<MfaSetupResponse>>();
        var sharedKey = setupPayload!.Data!.SharedKey;
        var totp = new Totp(Base32Encoding.ToBytes(sharedKey));

        var invalidEnableResponse = await client.PostAsJsonAsync("/api/auth/mfa/enable", new MfaVerifyRequest { Code = "000000" });
        invalidEnableResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var enableResponse = await client.PostAsJsonAsync("/api/auth/mfa/enable", new MfaVerifyRequest { Code = totp.ComputeTotp() });
        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var profileResponse = await client.GetAsync("/api/auth/me");
        var profilePayload = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<AuthenticatedUserDto>>();
        profilePayload!.Data!.IsMfaEnabled.Should().BeTrue();

        var disableResponse = await client.PostAsJsonAsync("/api/auth/mfa/disable", new MfaVerifyRequest { Code = totp.ComputeTotp() });
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var disabledProfileResponse = await client.GetAsync("/api/auth/me");
        var disabledProfilePayload = await disabledProfileResponse.Content.ReadFromJsonAsync<ApiResponse<AuthenticatedUserDto>>();
        disabledProfilePayload!.Data!.IsMfaEnabled.Should().BeFalse();
    }
}

internal static class AuthIntegrationTestsHelper
{
    public static async Task CreateUserAsync(TaxComplianceApiFactory factory, HttpClient client, string email, string displayName)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Infrastructure.Identity.ApplicationUser>>();
        var organizationId = dbContext.Organizations.Single().Id;
        var user = new Infrastructure.Identity.ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            LockoutEnabled = true,
            OrganizationId = organizationId
        };

        var result = await userManager.CreateAsync(user, "ValidPass123!");
        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    public static async Task<string> GetAccessTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return payload?.Data?.AccessToken ?? string.Empty;
    }
}
