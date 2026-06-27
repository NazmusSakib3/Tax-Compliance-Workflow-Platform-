using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskOccurrences;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Provides task occurrence workflow operations such as assignment, status updates, comments, documents, and audit history.
/// </summary>
[ApiController]
[Route("api/compliance-task-occurrences")]
[Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
public class TaskOccurrenceWorkflowController : ControllerBase
{
    private readonly IComplianceTaskOccurrenceWorkflowService workflowService;

    public TaskOccurrenceWorkflowController(IComplianceTaskOccurrenceWorkflowService workflowService)
    {
        this.workflowService = workflowService;
    }

    /// <summary>
    /// Returns all generated task occurrences.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ComplianceTaskOccurrenceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ComplianceTaskOccurrenceListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] bool assignedOnly = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await workflowService.GetPagedAsync(new TaskOccurrenceListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status,
            AssignedOnly = assignedOnly
        }, cancellationToken));
    }

    /// <summary>
    /// Returns a single task occurrence with rule and assignment details.
    /// </summary>
    [HttpGet("{occurrenceId:guid}")]
    [ProducesResponseType(typeof(ComplianceTaskOccurrenceDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceTaskOccurrenceDetailDto>> GetById(Guid occurrenceId, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.GetByIdAsync(occurrenceId, cancellationToken));
    }

    /// <summary>
    /// Returns users that can be selected as assignees.
    /// </summary>
    [HttpGet("assignable-users")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AssignableUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AssignableUserDto>>> GetAssignableUsers(CancellationToken cancellationToken)
    {
        return Ok(await workflowService.GetAssignableUsersAsync(cancellationToken));
    }

    /// <summary>
    /// Assigns the occurrence to a user.
    /// </summary>
    [HttpPost("{occurrenceId:guid}/assignment")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(ComplianceTaskOccurrenceDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceTaskOccurrenceDetailDto>> Assign(Guid occurrenceId, [FromBody] UpdateTaskOccurrenceAssignmentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.AssignAsync(occurrenceId, request, cancellationToken));
    }

    /// <summary>
    /// Changes the workflow status of the occurrence.
    /// </summary>
    [HttpPost("{occurrenceId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.ContributorAccess)]
    [ProducesResponseType(typeof(ComplianceTaskOccurrenceDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceTaskOccurrenceDetailDto>> ChangeStatus(Guid occurrenceId, [FromBody] UpdateTaskOccurrenceStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.ChangeStatusAsync(occurrenceId, request, cancellationToken));
    }

    /// <summary>
    /// Returns comments for the occurrence.
    /// </summary>
    [HttpGet("{occurrenceId:guid}/comments")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskCommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TaskCommentDto>>> GetComments(Guid occurrenceId, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.GetCommentsAsync(occurrenceId, cancellationToken));
    }

    /// <summary>
    /// Adds a comment to the occurrence.
    /// </summary>
    [HttpPost("{occurrenceId:guid}/comments")]
    [Authorize(Policy = AuthorizationPolicies.ContributorAccess)]
    [ProducesResponseType(typeof(TaskCommentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskCommentDto>> AddComment(Guid occurrenceId, [FromBody] CreateTaskCommentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.AddCommentAsync(occurrenceId, request, cancellationToken));
    }

    /// <summary>
    /// Returns uploaded documents for the occurrence.
    /// </summary>
    [HttpGet("{occurrenceId:guid}/documents")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskDocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TaskDocumentDto>>> GetDocuments(Guid occurrenceId, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.GetDocumentsAsync(occurrenceId, cancellationToken));
    }

    /// <summary>
    /// Uploads a document and links it to the occurrence.
    /// </summary>
    [HttpPost("{occurrenceId:guid}/documents")]
    [Authorize(Policy = AuthorizationPolicies.ContributorAccess)]
    [ProducesResponseType(typeof(TaskDocumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskDocumentDto>> UploadDocument(Guid occurrenceId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A non-empty file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await workflowService.AddDocumentAsync(occurrenceId, stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns audit log entries for the occurrence.
    /// </summary>
    [HttpGet("{occurrenceId:guid}/audit-log")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditLogEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AuditLogEntryDto>>> GetAuditLog(Guid occurrenceId, CancellationToken cancellationToken)
    {
        return Ok(await workflowService.GetAuditLogAsync(occurrenceId, cancellationToken));
    }

    /// <summary>
    /// Downloads a previously uploaded document.
    /// </summary>
    [HttpGet("documents/{documentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await workflowService.DownloadDocumentAsync(documentId, cancellationToken);
        return File(result.Stream, result.ContentType, result.FileName);
    }
}
