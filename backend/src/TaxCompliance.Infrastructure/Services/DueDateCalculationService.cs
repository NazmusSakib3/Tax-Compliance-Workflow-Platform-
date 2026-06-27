using TaxCompliance.Application.ComplianceTaskOccurrences;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;

namespace TaxCompliance.Infrastructure.Services;

public class DueDateCalculationService : IDueDateCalculationService
{
    public IReadOnlyCollection<OccurrenceScheduleItem> BuildSchedule(ComplianceTaskRule rule, DateOnly generationStartDate, DateOnly generationEndDate)
    {
        var schedule = new List<OccurrenceScheduleItem>();
        var startMonth = new DateOnly(generationStartDate.Year, generationStartDate.Month, 1);
        var endMonth = new DateOnly(generationEndDate.Year, generationEndDate.Month, 1);

        for (var cursor = startMonth; cursor <= endMonth; cursor = cursor.AddMonths(1))
        {
            if (!IsPeriodStart(rule.RecurrenceType, cursor))
            {
                continue;
            }

            var periodStart = cursor;
            var periodEnd = GetPeriodEnd(rule.RecurrenceType, cursor);
            var dueDate = GetDueDate(rule, periodEnd);

            if (dueDate < generationStartDate || dueDate > generationEndDate)
            {
                continue;
            }

            schedule.Add(new OccurrenceScheduleItem
            {
                PeriodStartDate = periodStart,
                PeriodEndDate = periodEnd,
                DueDate = dueDate
            });
        }

        return schedule;
    }

    private static bool IsPeriodStart(RecurrenceType recurrenceType, DateOnly monthStart)
    {
        return recurrenceType switch
        {
            RecurrenceType.Monthly => true,
            RecurrenceType.Quarterly => monthStart.Month is 1 or 4 or 7 or 10,
            RecurrenceType.Yearly => monthStart.Month == 1,
            _ => false
        };
    }

    private static DateOnly GetPeriodEnd(RecurrenceType recurrenceType, DateOnly periodStart)
    {
        return recurrenceType switch
        {
            RecurrenceType.Monthly => periodStart.AddMonths(1).AddDays(-1),
            RecurrenceType.Quarterly => periodStart.AddMonths(3).AddDays(-1),
            RecurrenceType.Yearly => periodStart.AddYears(1).AddDays(-1),
            _ => periodStart
        };
    }

    private static DateOnly GetDueDate(ComplianceTaskRule rule, DateOnly periodEnd)
    {
        return rule.RecurrenceType switch
        {
            RecurrenceType.Monthly => CreateClampedDate(periodEnd.Year, periodEnd.Month, rule.DueDayOfMonth),
            RecurrenceType.Quarterly => CreateClampedDate(periodEnd.Year, periodEnd.Month, rule.DueDayOfMonth),
            RecurrenceType.Yearly => CreateClampedDate(periodEnd.Year, rule.DueMonthOfYear ?? periodEnd.Month, rule.DueDayOfMonth),
            _ => periodEnd
        };
    }

    private static DateOnly CreateClampedDate(int year, int month, int day)
    {
        var lastDayOfMonth = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, lastDayOfMonth));
    }
}

