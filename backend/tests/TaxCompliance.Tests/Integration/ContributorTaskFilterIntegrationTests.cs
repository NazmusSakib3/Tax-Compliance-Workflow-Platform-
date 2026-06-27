using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskOccurrences;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Tests.Integration;

public class ContributorTaskFilterIntegrationTests : IClassFixture<TaxComplianceApiFactory>
{
    private readonly TaxComplianceApiFactory factory;

    public ContributorTaskFilterIntegrationTests(TaxComplianceApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Contributor_ShouldOnlySeeAssignedTaskOccurrences()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var organization = dbContext.Organizations.First();
        var contributor = new ApplicationUser
        {
            UserName = "contributor.filter@example.com",
            Email = "contributor.filter@example.com",
            DisplayName = "Filter Contributor",
            EmailConfirmed = true,
            OrganizationId = organization.Id
        };
        await userManager.CreateAsync(contributor, "Contributor123!");
        await userManager.AddToRoleAsync(contributor, RoleNames.Contributor);

        var legalEntity = new LegalEntity
        {
            OrganizationId = organization.Id,
            Name = "Filter Test Entity",
            RegistrationNumber = "FILTER-REG",
            TaxIdentifier = "FILTER-TAX"
        };
        var jurisdiction = new Jurisdiction
        {
            OrganizationId = organization.Id,
            Name = "Filter Jurisdiction",
            CountryCode = "US",
            RegionCode = "TX",
            FilingAuthority = "Texas Comptroller"
        };
        var template = new ComplianceTemplate
        {
            OrganizationId = organization.Id,
            Name = "Filter Template",
            FilingType = "Sales Tax"
        };
        var rule = new ComplianceTaskRule
        {
            LegalEntityId = legalEntity.Id,
            JurisdictionId = jurisdiction.Id,
            ComplianceTemplateId = template.Id,
            Title = "Filter Rule",
            RecurrenceType = RecurrenceType.Monthly,
            DueDayOfMonth = 10,
            LegalEntity = legalEntity,
            Jurisdiction = jurisdiction,
            ComplianceTemplate = template
        };

        dbContext.LegalEntities.Add(legalEntity);
        dbContext.Jurisdictions.Add(jurisdiction);
        dbContext.ComplianceTemplates.Add(template);
        dbContext.ComplianceTaskRules.Add(rule);

        var assignedOccurrence = new ComplianceTaskOccurrence
        {
            ComplianceTaskRuleId = rule.Id,
            ComplianceTaskRule = rule,
            AssignedToUserId = contributor.Id,
            PeriodStartDate = new DateOnly(2026, 6, 1),
            PeriodEndDate = new DateOnly(2026, 6, 30),
            DueDate = new DateOnly(2026, 6, 15),
            Status = ComplianceTaskOccurrenceStatus.Pending
        };
        var otherOccurrence = new ComplianceTaskOccurrence
        {
            ComplianceTaskRuleId = rule.Id,
            AssignedToUserId = "someone-else",
            PeriodStartDate = new DateOnly(2026, 7, 1),
            PeriodEndDate = new DateOnly(2026, 7, 31),
            DueDate = new DateOnly(2026, 7, 15),
            Status = ComplianceTaskOccurrenceStatus.Pending
        };

        dbContext.ComplianceTaskOccurrences.AddRange(assignedOccurrence, otherOccurrence);
        await dbContext.SaveChangesAsync();

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = contributor.Email,
            Password = "Contributor123!"
        });
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Data!.AccessToken);

        var listResponse = await client.GetAsync("/api/compliance-task-occurrences?page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var occurrences = await listResponse.Content.ReadFromJsonAsync<PagedResult<ComplianceTaskOccurrenceListItemDto>>();
        occurrences!.Items.Should().ContainSingle(item => item.Id == assignedOccurrence.Id);
        occurrences.Items.Should().NotContain(item => item.Id == otherOccurrence.Id);
    }
}
