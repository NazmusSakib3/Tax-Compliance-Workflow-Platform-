using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskOccurrences;

namespace TaxCompliance.Application.ComplianceTaskOccurrences;

public class TaskOccurrenceListQuery : PagedListQuery
{
    public string? Status { get; set; }

    public bool AssignedOnly { get; set; }
}

public interface IComplianceTaskOccurrenceWorkflowService
{
    Task<PagedResult<ComplianceTaskOccurrenceListItemDto>> GetPagedAsync(TaskOccurrenceListQuery query, CancellationToken cancellationToken);
    Task<ComplianceTaskOccurrenceDetailDto> GetByIdAsync(Guid occurrenceId, CancellationToken cancellationToken);
    Task<ComplianceTaskOccurrenceDetailDto> AssignAsync(Guid occurrenceId, UpdateTaskOccurrenceAssignmentRequest request, CancellationToken cancellationToken);
    Task<ComplianceTaskOccurrenceDetailDto> ChangeStatusAsync(Guid occurrenceId, UpdateTaskOccurrenceStatusRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TaskCommentDto>> GetCommentsAsync(Guid occurrenceId, CancellationToken cancellationToken);
    Task<TaskCommentDto> AddCommentAsync(Guid occurrenceId, CreateTaskCommentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TaskDocumentDto>> GetDocumentsAsync(Guid occurrenceId, CancellationToken cancellationToken);
    Task<TaskDocumentDto> AddDocumentAsync(Guid occurrenceId, Stream stream, string fileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken);
    Task<(Stream Stream, string FileName, string ContentType)> DownloadDocumentAsync(Guid documentId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditLogEntryDto>> GetAuditLogAsync(Guid occurrenceId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AssignableUserDto>> GetAssignableUsersAsync(CancellationToken cancellationToken);
}
