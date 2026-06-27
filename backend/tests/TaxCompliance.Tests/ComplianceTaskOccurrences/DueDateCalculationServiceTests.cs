using FluentAssertions;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.Services;

namespace TaxCompliance.Tests.ComplianceTaskOccurrences;

public class DueDateCalculationServiceTests
{
    private readonly DueDateCalculationService service = new();

    [Fact]
    public void BuildSchedule_ShouldGenerateMonthlyOccurrencesWithinWindow()
    {
        var rule = new ComplianceTaskRule
        {
            RecurrenceType = RecurrenceType.Monthly,
            DueDayOfMonth = 10
        };

        var schedule = service.BuildSchedule(rule, new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30));

        schedule.Should().HaveCount(3);
        schedule.Select(item => item.DueDate).Should().ContainInOrder(
            new DateOnly(2026, 4, 10),
            new DateOnly(2026, 5, 10),
            new DateOnly(2026, 6, 10));
    }

    [Fact]
    public void BuildSchedule_ShouldGenerateQuarterlyOccurrencesFromQuarterStart()
    {
        var rule = new ComplianceTaskRule
        {
            RecurrenceType = RecurrenceType.Quarterly,
            DueDayOfMonth = 15
        };

        var schedule = service.BuildSchedule(rule, new DateOnly(2026, 4, 1), new DateOnly(2026, 12, 31));

        schedule.Should().HaveCount(3);
        schedule.Select(item => item.PeriodStartDate).Should().ContainInOrder(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 10, 1));
        schedule.Select(item => item.DueDate).Should().ContainInOrder(
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 9, 15),
            new DateOnly(2026, 12, 15));
    }

    [Fact]
    public void BuildSchedule_ShouldGenerateYearlyOccurrenceUsingConfiguredMonthAndClampedDay()
    {
        var rule = new ComplianceTaskRule
        {
            RecurrenceType = RecurrenceType.Yearly,
            DueDayOfMonth = 31,
            DueMonthOfYear = 2
        };

        var schedule = service.BuildSchedule(rule, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        schedule.Should().ContainSingle();
        schedule.Single().DueDate.Should().Be(new DateOnly(2026, 2, 28));
        schedule.Single().PeriodStartDate.Should().Be(new DateOnly(2026, 1, 1));
        schedule.Single().PeriodEndDate.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void BuildSchedule_ShouldSkipPeriodsWhoseDueDateFallsOutsideWindow()
    {
        var rule = new ComplianceTaskRule
        {
            RecurrenceType = RecurrenceType.Monthly,
            DueDayOfMonth = 5
        };

        var schedule = service.BuildSchedule(rule, new DateOnly(2026, 4, 10), new DateOnly(2026, 5, 31));

        schedule.Should().ContainSingle();
        schedule.Single().DueDate.Should().Be(new DateOnly(2026, 5, 5));
    }
}

