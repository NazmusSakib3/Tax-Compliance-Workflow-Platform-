namespace TaxCompliance.Application.AuditLog;

public class GlobalAuditLogEntryDto
{
    public Guid Id { get; set; }
    public Guid ComplianceTaskOccurrenceId { get; set; }
    public string RuleTitle { get; set; } = string.Empty;
    public string LegalEntityName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public string PerformedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public class AuditLogQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? ActionType { get; set; }
}
