using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class TaskComment : BaseEntity
{
    public Guid ComplianceTaskOccurrenceId { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public ComplianceTaskOccurrence? ComplianceTaskOccurrence { get; set; }
}

