using System.ComponentModel.DataAnnotations;
using TaxCompliance.Domain.Enums;

namespace TaxCompliance.Application.ComplianceTaskOccurrences;

public class ComplianceTaskOccurrenceListItemDto
{
    public Guid Id { get; set; }
    public Guid ComplianceTaskRuleId { get; set; }
    public string RuleTitle { get; set; } = string.Empty;
    public string LegalEntityName { get; set; } = string.Empty;
    public string JurisdictionName { get; set; } = string.Empty;
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public DateOnly DueDate { get; set; }
    public ComplianceTaskOccurrenceStatus Status { get; set; }
    public string AssignedToUserId { get; set; } = string.Empty;
    public string AssignedToDisplayName { get; set; } = string.Empty;
}

public class ComplianceTaskOccurrenceDetailDto : ComplianceTaskOccurrenceListItemDto
{
    public string RuleDescription { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
}

public class TaskCommentDto
{
    public Guid Id { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public class TaskDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public string UploadedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public class AuditLogEntryDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public string PerformedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public class AssignableUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateTaskOccurrenceAssignmentRequest
{
    [Required]
    public string AssignedToUserId { get; set; } = string.Empty;
}

public class UpdateTaskOccurrenceStatusRequest
{
    [Required]
    public ComplianceTaskOccurrenceStatus Status { get; set; }
}

public class CreateTaskCommentRequest
{
    [Required]
    [StringLength(2000)]
    public string CommentText { get; set; } = string.Empty;
}

