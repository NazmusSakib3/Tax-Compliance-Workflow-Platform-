using TaxCompliance.Domain.Common;
using TaxCompliance.Domain.Enums;

namespace TaxCompliance.Domain.Entities;

public class ComplianceTaskOccurrence : BaseEntity
{
    public Guid ComplianceTaskRuleId { get; set; }
    public string AssignedToUserId { get; set; } = string.Empty;
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public DateOnly DueDate { get; set; }
    public ComplianceTaskOccurrenceStatus Status { get; set; } = ComplianceTaskOccurrenceStatus.Pending;
    public ComplianceTaskRule? ComplianceTaskRule { get; set; }
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskDocument> Documents { get; set; } = new List<TaskDocument>();
    public ICollection<AuditLogEntry> AuditLogEntries { get; set; } = new List<AuditLogEntry>();
}
