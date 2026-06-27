using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaxCompliance.Application.ComplianceTaskOccurrences;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.Persistence;
using TaxCompliance.Infrastructure.Services;
using TaxCompliance.Tests.TestDoubles;

namespace TaxCompliance.Tests.ComplianceTaskOccurrences;

public class ComplianceTaskOccurrenceGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_ShouldCreateOccurrencesOnlyForMissingPeriods()
    {
        await using var dbContext = BuildDbContext();
        var rule = new ComplianceTaskRule
        {
            Id = Guid.NewGuid(),
            Title = "Monthly VAT",
            LegalEntityId = Guid.NewGuid(),
            JurisdictionId = Guid.NewGuid(),
            ComplianceTemplateId = Guid.NewGuid(),
            RecurrenceType = RecurrenceType.Monthly,
            DueDayOfMonth = DateOnly.FromDateTime(DateTime.UtcNow).Day,
            IsActive = true
        };

        dbContext.ComplianceTaskRules.Add(rule);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        dbContext.ComplianceTaskOccurrences.Add(new ComplianceTaskOccurrence
        {
            ComplianceTaskRuleId = rule.Id,
            PeriodStartDate = new DateOnly(today.Year, today.Month, 1),
            PeriodEndDate = new DateOnly(today.Year, today.Month, 1).AddMonths(1).AddDays(-1),
            DueDate = today,
            Status = ComplianceTaskOccurrenceStatus.Pending
        });

        await dbContext.SaveChangesAsync();

        var service = new ComplianceTaskOccurrenceGenerationService(
            dbContext,
            new DueDateCalculationService(),
            Options.Create(new OccurrenceGenerationOptions { MonthsAheadToGenerate = 2 }),
            new FakeCurrentUserContextService(),
            new FakeDashboardCacheInvalidationService());

        var result = await service.GenerateAsync(CancellationToken.None);

        result.DuplicateOccurrencesSkipped.Should().BeGreaterThan(0);
        result.OccurrencesCreated.Should().BeGreaterThan(0);
        dbContext.ComplianceTaskOccurrences.Count().Should().Be(result.OccurrencesCreated + 1);
        dbContext.AuditLogEntries.Count().Should().Be(result.OccurrencesCreated);
    }

    [Fact]
    public async Task GenerateAsync_ShouldWriteSystemAuditIdentityWhenNoCurrentUserIsAvailable()
    {
        await using var dbContext = BuildDbContext();
        dbContext.ComplianceTaskRules.Add(new ComplianceTaskRule
        {
            Id = Guid.NewGuid(),
            Title = "Monthly VAT",
            LegalEntityId = Guid.NewGuid(),
            JurisdictionId = Guid.NewGuid(),
            ComplianceTemplateId = Guid.NewGuid(),
            RecurrenceType = RecurrenceType.Monthly,
            DueDayOfMonth = DateOnly.FromDateTime(DateTime.UtcNow).Day,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var service = new ComplianceTaskOccurrenceGenerationService(
            dbContext,
            new DueDateCalculationService(),
            Options.Create(new OccurrenceGenerationOptions { MonthsAheadToGenerate = 2 }),
            new FakeCurrentUserContextService(),
            new FakeDashboardCacheInvalidationService());

        var result = await service.GenerateAsync(CancellationToken.None);

        result.OccurrencesCreated.Should().BeGreaterThan(0);
        dbContext.AuditLogEntries.Should().OnlyContain(entry =>
            entry.PerformedByUserId == "system" &&
            entry.PerformedByDisplayName == "System");
    }

    private static ApplicationDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
