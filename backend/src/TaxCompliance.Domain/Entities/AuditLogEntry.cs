using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class AuditLogEntry : BaseEntity
{
    public Guid ComplianceTaskOccurrenceId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public string PerformedByDisplayName { get; set; } = string.Empty;
    public ComplianceTaskOccurrence? ComplianceTaskOccurrence { get; set; }
}

