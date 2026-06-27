namespace TaxCompliance.Application.ComplianceTaskOccurrences;

public interface IComplianceTaskOccurrenceGenerationService
{
    Task<OccurrenceGenerationResultDto> GenerateAsync(CancellationToken cancellationToken);
}
