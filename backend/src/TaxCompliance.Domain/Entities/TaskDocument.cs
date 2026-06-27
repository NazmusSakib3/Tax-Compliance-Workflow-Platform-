using TaxCompliance.Domain.Common;

namespace TaxCompliance.Domain.Entities;

public class TaskDocument : BaseEntity
{
    public Guid ComplianceTaskOccurrenceId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public string UploadedByDisplayName { get; set; } = string.Empty;
    public ComplianceTaskOccurrence? ComplianceTaskOccurrence { get; set; }
}

