using TaxCompliance.Domain.Entities;

namespace TaxCompliance.Application.ComplianceTaskOccurrences;

public interface IDueDateCalculationService
{
    IReadOnlyCollection<OccurrenceScheduleItem> BuildSchedule(ComplianceTaskRule rule, DateOnly generationStartDate, DateOnly generationEndDate);
}

