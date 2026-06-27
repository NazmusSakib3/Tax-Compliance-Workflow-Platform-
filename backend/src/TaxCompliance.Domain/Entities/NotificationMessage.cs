using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class NotificationMessage : BaseEntity
{
    public Guid ComplianceTaskOccurrenceId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedUtc { get; set; }
    public ComplianceTaskOccurrence? ComplianceTaskOccurrence { get; set; }
}

