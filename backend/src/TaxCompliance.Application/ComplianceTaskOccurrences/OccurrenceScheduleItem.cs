namespace TaxCompliance.Application.ComplianceTaskOccurrences;

public class OccurrenceScheduleItem
{
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public DateOnly DueDate { get; set; }
}

