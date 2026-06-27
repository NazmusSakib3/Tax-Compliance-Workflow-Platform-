namespace TaxCompliance.Application.ComplianceTaskOccurrences;

public class OccurrenceGenerationResultDto
{
    public int RulesEvaluated { get; set; }
    public int OccurrencesCreated { get; set; }
    public int DuplicateOccurrencesSkipped { get; set; }
}

