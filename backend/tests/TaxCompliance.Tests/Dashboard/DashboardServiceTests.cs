using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using TaxCompliance.Application.Auth;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.Persistence;
using TaxCompliance.Infrastructure.Services;
using TaxCompliance.Tests.TestDoubles;

namespace TaxCompliance.Tests.Dashboard;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ShouldReturnExpectedCounts()
    {
        await using var dbContext = BuildDbContext();
        SeedOccurrences(dbContext);

        var service = new DashboardService(
            dbContext,
            BuildDistributedCache(),
            new FakeOrganizationScopeService(),
            new FakeCurrentUserContextService
            {
                CurrentUser = new CurrentUserContext { Roles = [RoleNames.Admin] }
            });

        var summary = await service.GetSummaryAsync(CancellationToken.None);

        summary.OverdueCount.Should().Be(1);
        summary.DueSoonCount.Should().Be(2);
        summary.CompletedCount.Should().Be(1);
        summary.InProgressCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldReturnCachedSummaryUntilCacheIsInvalidated()
    {
        await using var dbContext = BuildDbContext();
        SeedOccurrences(dbContext);
        var cache = BuildDistributedCache();
        var service = new DashboardService(
            dbContext,
            cache,
            new FakeOrganizationScopeService(),
            new FakeCurrentUserContextService
            {
                CurrentUser = new CurrentUserContext { Roles = [RoleNames.Admin] }
            });

        var firstSummary = await service.GetSummaryAsync(CancellationToken.None);

        dbContext.ComplianceTaskOccurrences.Add(new ComplianceTaskOccurrence
        {
            ComplianceTaskRuleId = Guid.NewGuid(),
            AssignedToUserId = string.Empty,
            PeriodStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PeriodEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            Status = ComplianceTaskOccurrenceStatus.Pending
        });
        await dbContext.SaveChangesAsync();

        var secondSummary = await service.GetSummaryAsync(CancellationToken.None);

        secondSummary.DueSoonCount.Should().Be(firstSummary.DueSoonCount);
    }

    private static ApplicationDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IDistributedCache BuildDistributedCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        return services.BuildServiceProvider().GetRequiredService<IDistributedCache>();
    }

    private static void SeedOccurrences(ApplicationDbContext dbContext)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        dbContext.ComplianceTaskOccurrences.AddRange(
            new ComplianceTaskOccurrence
            {
                ComplianceTaskRuleId = Guid.NewGuid(),
                AssignedToUserId = string.Empty,
                PeriodStartDate = today.AddMonths(-1),
                PeriodEndDate = today.AddDays(-10),
                DueDate = today.AddDays(-1),
                Status = ComplianceTaskOccurrenceStatus.Pending
            },
            new ComplianceTaskOccurrence
            {
                ComplianceTaskRuleId = Guid.NewGuid(),
                AssignedToUserId = string.Empty,
                PeriodStartDate = today,
                PeriodEndDate = today.AddDays(7),
                DueDate = today.AddDays(3),
                Status = ComplianceTaskOccurrenceStatus.Pending
            },
            new ComplianceTaskOccurrence
            {
                ComplianceTaskRuleId = Guid.NewGuid(),
                AssignedToUserId = string.Empty,
                PeriodStartDate = today,
                PeriodEndDate = today.AddDays(7),
                DueDate = today.AddDays(5),
                Status = ComplianceTaskOccurrenceStatus.InProgress
            },
            new ComplianceTaskOccurrence
            {
                ComplianceTaskRuleId = Guid.NewGuid(),
                AssignedToUserId = string.Empty,
                PeriodStartDate = today.AddMonths(-2),
                PeriodEndDate = today.AddMonths(-1),
                DueDate = today.AddDays(-15),
                Status = ComplianceTaskOccurrenceStatus.Completed
            },
            new ComplianceTaskOccurrence
            {
                ComplianceTaskRuleId = Guid.NewGuid(),
                AssignedToUserId = string.Empty,
                PeriodStartDate = today,
                PeriodEndDate = today.AddDays(7),
                DueDate = today.AddDays(1),
                Status = ComplianceTaskOccurrenceStatus.Cancelled
            });

        dbContext.SaveChanges();
    }
}
