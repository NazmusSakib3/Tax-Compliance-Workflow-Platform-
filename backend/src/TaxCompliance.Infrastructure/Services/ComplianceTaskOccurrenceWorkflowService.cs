using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskOccurrences;
using TaxCompliance.Application.Dashboard;
using TaxCompliance.Application.FileStorage;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.FileStorage;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class ComplianceTaskOccurrenceWorkflowService : IComplianceTaskOccurrenceWorkflowService
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ICurrentUserContextService currentUserContextService;
    private readonly IOrganizationScopeService organizationScope;
    private readonly IFileStorageService fileStorageService;
    private readonly IDashboardCacheInvalidationService dashboardCacheInvalidationService;
    private readonly LocalFileStorageOptions fileStorageOptions;

    public ComplianceTaskOccurrenceWorkflowService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ICurrentUserContextService currentUserContextService,
        IOrganizationScopeService organizationScope,
        IFileStorageService fileStorageService,
        IDashboardCacheInvalidationService dashboardCacheInvalidationService,
        IOptions<LocalFileStorageOptions> fileStorageOptions)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.currentUserContextService = currentUserContextService;
        this.organizationScope = organizationScope;
        this.fileStorageService = fileStorageService;
        this.dashboardCacheInvalidationService = dashboardCacheInvalidationService;
        this.fileStorageOptions = fileStorageOptions.Value;
    }

    public async Task<PagedResult<ComplianceTaskOccurrenceListItemDto>> GetPagedAsync(TaskOccurrenceListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationHelper.Normalize(query.Page, query.PageSize);
        var currentUser = currentUserContextService.GetCurrentUser();

        var occurrenceQuery = dbContext.ComplianceTaskOccurrences
            .AsNoTracking()
            .Include(occurrence => occurrence.ComplianceTaskRule)!.ThenInclude(rule => rule!.LegalEntity)
            .Include(occurrence => occurrence.ComplianceTaskRule)!.ThenInclude(rule => rule!.Jurisdiction)
            .AsQueryable();

        occurrenceQuery = occurrenceQuery.ApplyOrganizationScope(
            organizationScope,
            currentUserContextService,
            occurrence => occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId);

        var isContributorOnly = currentUser.Roles.Contains(RoleNames.Contributor) &&
            !currentUser.Roles.Contains(RoleNames.Admin) &&
            !currentUser.Roles.Contains(RoleNames.ComplianceManager);

        if (isContributorOnly || query.AssignedOnly)
        {
            occurrenceQuery = occurrenceQuery.Where(occurrence => occurrence.AssignedToUserId == currentUser.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            occurrenceQuery = occurrenceQuery.Where(occurrence =>
                occurrence.ComplianceTaskRule!.Title.Contains(search) ||
                occurrence.ComplianceTaskRule.LegalEntity!.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<ComplianceTaskOccurrenceStatus>(query.Status, true, out var status))
        {
            occurrenceQuery = occurrenceQuery.Where(occurrence => occurrence.Status == status);
        }

        var totalCount = await occurrenceQuery.CountAsync(cancellationToken);
        var occurrences = await occurrenceQuery
            .OrderBy(occurrence => occurrence.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var assignedUserIds = occurrences
            .Where(occurrence => !string.IsNullOrWhiteSpace(occurrence.AssignedToUserId))
            .Select(occurrence => occurrence.AssignedToUserId)
            .Distinct()
            .ToArray();

        var assigneeLookup = await userManager.Users
            .Where(user => assignedUserIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        var items = occurrences
            .Select(occurrence => new ComplianceTaskOccurrenceListItemDto
            {
                Id = occurrence.Id,
                ComplianceTaskRuleId = occurrence.ComplianceTaskRuleId,
                RuleTitle = occurrence.ComplianceTaskRule!.Title,
                LegalEntityName = occurrence.ComplianceTaskRule.LegalEntity!.Name,
                JurisdictionName = occurrence.ComplianceTaskRule.Jurisdiction!.Name,
                PeriodStartDate = occurrence.PeriodStartDate,
                PeriodEndDate = occurrence.PeriodEndDate,
                DueDate = occurrence.DueDate,
                Status = occurrence.Status,
                AssignedToUserId = occurrence.AssignedToUserId,
                AssignedToDisplayName = assigneeLookup.TryGetValue(occurrence.AssignedToUserId, out var displayName) ? displayName : string.Empty
            })
            .ToList();

        return PaginationHelper.Create(items, page, pageSize, totalCount);
    }

    public async Task<ComplianceTaskOccurrenceDetailDto> GetByIdAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        var occurrence = await LoadOccurrenceAsync(occurrenceId, cancellationToken);
        EnsureContributorReadAccess(occurrence);
        return await MapDetailAsync(occurrence, cancellationToken);
    }

    public async Task<ComplianceTaskOccurrenceDetailDto> AssignAsync(Guid occurrenceId, UpdateTaskOccurrenceAssignmentRequest request, CancellationToken cancellationToken)
    {
        var occurrence = await LoadOccurrenceAsync(occurrenceId, cancellationToken);
        EnsureManagerAccess();

        var assignee = await userManager.FindByIdAsync(request.AssignedToUserId);
        if (assignee is null)
        {
            throw new AppValidationException("Assigned user is invalid.", new Dictionary<string, string[]>
            {
                ["assignedToUserId"] = ["The selected user does not exist."]
            });
        }

        var occurrenceOrganizationId = occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId;
        if (!assignee.OrganizationId.HasValue || assignee.OrganizationId.Value != occurrenceOrganizationId)
        {
            throw new AppValidationException("Assigned user is invalid.", new Dictionary<string, string[]>
            {
                ["assignedToUserId"] = ["The selected user does not belong to this task's organization."]
            });
        }

        var previousAssignee = occurrence.AssignedToUserId;
        occurrence.AssignedToUserId = assignee.Id;
        occurrence.UpdatedUtc = DateTime.UtcNow;

        AddAuditLog(
            occurrence,
            "AssignmentChanged",
            $"Assignment changed from '{previousAssignee}' to '{assignee.DisplayName}'.");

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCacheInvalidationService.InvalidateAsync(cancellationToken);
        return await MapDetailAsync(occurrence, cancellationToken);
    }

    public async Task<ComplianceTaskOccurrenceDetailDto> ChangeStatusAsync(Guid occurrenceId, UpdateTaskOccurrenceStatusRequest request, CancellationToken cancellationToken)
    {
        var occurrence = await LoadOccurrenceAsync(occurrenceId, cancellationToken);
        EnsureOccurrenceWriteAccess(occurrence);

        if (!AllowedTransitions.TryGetValue(occurrence.Status, out var validStatuses) || !validStatuses.Contains(request.Status))
        {
            throw new AppValidationException($"Status transition from {occurrence.Status} to {request.Status} is not allowed.");
        }

        var previousStatus = occurrence.Status;
        occurrence.Status = request.Status;
        occurrence.UpdatedUtc = DateTime.UtcNow;

        AddAuditLog(
            occurrence,
            "StatusChanged",
            $"Status changed from '{previousStatus}' to '{request.Status}'.");

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCacheInvalidationService.InvalidateAsync(cancellationToken);
        return await MapDetailAsync(occurrence, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskCommentDto>> GetCommentsAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        await LoadOccurrenceForReadAsync(occurrenceId, cancellationToken);

        return await dbContext.TaskComments
            .Where(comment => comment.ComplianceTaskOccurrenceId == occurrenceId)
            .OrderBy(comment => comment.CreatedUtc)
            .Select(comment => new TaskCommentDto
            {
                Id = comment.Id,
                CommentText = comment.CommentText,
                CreatedByUserId = comment.CreatedByUserId,
                CreatedByDisplayName = comment.CreatedByDisplayName,
                CreatedUtc = comment.CreatedUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskCommentDto> AddCommentAsync(Guid occurrenceId, CreateTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var occurrence = await LoadOccurrenceAsync(occurrenceId, cancellationToken);
        EnsureOccurrenceWriteAccess(occurrence);

        var currentUser = currentUserContextService.GetCurrentUser();
        var comment = new TaskComment
        {
            ComplianceTaskOccurrenceId = occurrenceId,
            CommentText = request.CommentText.Trim(),
            CreatedByUserId = currentUser.UserId,
            CreatedByDisplayName = currentUser.DisplayName
        };

        dbContext.TaskComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCacheInvalidationService.InvalidateAsync(cancellationToken);

        return new TaskCommentDto
        {
            Id = comment.Id,
            CommentText = comment.CommentText,
            CreatedByUserId = comment.CreatedByUserId,
            CreatedByDisplayName = comment.CreatedByDisplayName,
            CreatedUtc = comment.CreatedUtc
        };
    }

    public async Task<IReadOnlyCollection<TaskDocumentDto>> GetDocumentsAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        await LoadOccurrenceForReadAsync(occurrenceId, cancellationToken);

        return await dbContext.TaskDocuments
            .Where(document => document.ComplianceTaskOccurrenceId == occurrenceId)
            .OrderByDescending(document => document.CreatedUtc)
            .Select(document => new TaskDocumentDto
            {
                Id = document.Id,
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSizeBytes = document.FileSizeBytes,
                UploadedByUserId = document.UploadedByUserId,
                UploadedByDisplayName = document.UploadedByDisplayName,
                CreatedUtc = document.CreatedUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskDocumentDto> AddDocumentAsync(Guid occurrenceId, Stream stream, string fileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken)
    {
        var occurrence = await LoadOccurrenceAsync(occurrenceId, cancellationToken);
        EnsureOccurrenceWriteAccess(occurrence);
        ValidateUploadedFile(fileName, fileSizeBytes);

        var currentUser = currentUserContextService.GetCurrentUser();
        var storedPath = await fileStorageService.SaveAsync(stream, fileName, cancellationToken);

        var document = new TaskDocument
        {
            ComplianceTaskOccurrenceId = occurrenceId,
            FileName = fileName,
            StoredPath = storedPath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedByUserId = currentUser.UserId,
            UploadedByDisplayName = currentUser.DisplayName
        };

        dbContext.TaskDocuments.Add(document);
        AddAuditLog(occurrence, "DocumentUploaded", $"Document '{fileName}' uploaded.");
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCacheInvalidationService.InvalidateAsync(cancellationToken);

        return new TaskDocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedByUserId = document.UploadedByUserId,
            UploadedByDisplayName = document.UploadedByDisplayName,
            CreatedUtc = document.CreatedUtc
        };
    }

    public async Task<(Stream Stream, string FileName, string ContentType)> DownloadDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.TaskDocuments
            .Include(item => item.ComplianceTaskOccurrence)!.ThenInclude(occurrence => occurrence!.ComplianceTaskRule)!.ThenInclude(rule => rule!.LegalEntity)
            .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken)
            ?? throw new EntityNotFoundException("Document was not found.");
        var occurrence = document.ComplianceTaskOccurrence
            ?? throw new EntityNotFoundException("Task occurrence was not found.");

        EnsureOccurrenceAccess(occurrence);
        EnsureContributorReadAccess(occurrence);

        var stream = await fileStorageService.OpenReadAsync(document.StoredPath, cancellationToken);
        return (stream, document.FileName, document.ContentType);
    }

    public async Task<IReadOnlyCollection<AuditLogEntryDto>> GetAuditLogAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        await LoadOccurrenceForReadAsync(occurrenceId, cancellationToken);

        return await dbContext.AuditLogEntries
            .Where(entry => entry.ComplianceTaskOccurrenceId == occurrenceId)
            .OrderByDescending(entry => entry.CreatedUtc)
            .Select(entry => new AuditLogEntryDto
            {
                Id = entry.Id,
                ActionType = entry.ActionType,
                Description = entry.Description,
                PerformedByUserId = entry.PerformedByUserId,
                PerformedByDisplayName = entry.PerformedByDisplayName,
                CreatedUtc = entry.CreatedUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AssignableUserDto>> GetAssignableUsersAsync(CancellationToken cancellationToken)
    {
        var userQuery = userManager.Users.AsQueryable();

        if (organizationScope.GetOrganizationId() is Guid organizationId)
        {
            userQuery = userQuery.Where(user => user.OrganizationId == organizationId);
        }
        else if (!OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser()))
        {
            organizationScope.RequireOrganizationId();
        }

        return await userQuery
            .OrderBy(user => user.DisplayName)
            .Select(user => new AssignableUserDto
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<ComplianceTaskOccurrence> LoadOccurrenceAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.ComplianceTaskOccurrences
            .Include(occurrence => occurrence.ComplianceTaskRule)!.ThenInclude(rule => rule!.LegalEntity)
            .Include(occurrence => occurrence.ComplianceTaskRule)!.ThenInclude(rule => rule!.Jurisdiction)
            .Include(occurrence => occurrence.ComplianceTaskRule)!.ThenInclude(rule => rule!.ComplianceTemplate)
            .SingleOrDefaultAsync(occurrence => occurrence.Id == occurrenceId, cancellationToken)
            ?? throw new EntityNotFoundException("Task occurrence was not found.");

        EnsureOccurrenceAccess(occurrence);
        return occurrence;
    }

    private void EnsureOccurrenceAccess(ComplianceTaskOccurrence occurrence)
    {
        organizationScope.EnsureSameOrganization(occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId);
    }

    private void EnsureContributorReadAccess(ComplianceTaskOccurrence occurrence)
    {
        var currentUser = currentUserContextService.GetCurrentUser();
        if (currentUser.Roles.Contains(RoleNames.Contributor) &&
            !currentUser.Roles.Contains(RoleNames.Admin) &&
            !currentUser.Roles.Contains(RoleNames.ComplianceManager) &&
            occurrence.AssignedToUserId != currentUser.UserId)
        {
            throw new EntityNotFoundException("Task occurrence was not found.");
        }
    }

    private async Task<ComplianceTaskOccurrence> LoadOccurrenceForReadAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        var occurrence = await LoadOccurrenceAsync(occurrenceId, cancellationToken);
        EnsureContributorReadAccess(occurrence);
        return occurrence;
    }

    private async Task<ComplianceTaskOccurrenceDetailDto> MapDetailAsync(ComplianceTaskOccurrence occurrence, CancellationToken cancellationToken)
    {
        var assignee = string.IsNullOrWhiteSpace(occurrence.AssignedToUserId)
            ? null
            : await userManager.FindByIdAsync(occurrence.AssignedToUserId);

        return new ComplianceTaskOccurrenceDetailDto
        {
            Id = occurrence.Id,
            ComplianceTaskRuleId = occurrence.ComplianceTaskRuleId,
            RuleTitle = occurrence.ComplianceTaskRule?.Title ?? string.Empty,
            RuleDescription = occurrence.ComplianceTaskRule?.Description ?? string.Empty,
            LegalEntityName = occurrence.ComplianceTaskRule?.LegalEntity?.Name ?? string.Empty,
            JurisdictionName = occurrence.ComplianceTaskRule?.Jurisdiction?.Name ?? string.Empty,
            TemplateName = occurrence.ComplianceTaskRule?.ComplianceTemplate?.Name ?? string.Empty,
            PeriodStartDate = occurrence.PeriodStartDate,
            PeriodEndDate = occurrence.PeriodEndDate,
            DueDate = occurrence.DueDate,
            Status = occurrence.Status,
            AssignedToUserId = occurrence.AssignedToUserId,
            AssignedToDisplayName = assignee?.DisplayName ?? string.Empty
        };
    }

    private void EnsureManagerAccess()
    {
        var currentUser = currentUserContextService.GetCurrentUser();
        if (!currentUser.Roles.Contains(RoleNames.Admin) && !currentUser.Roles.Contains(RoleNames.ComplianceManager))
        {
            throw new AppValidationException("Only administrators and compliance managers can change assignment.");
        }
    }

    private void EnsureOccurrenceWriteAccess(ComplianceTaskOccurrence occurrence)
    {
        var currentUser = currentUserContextService.GetCurrentUser();
        if (currentUser.Roles.Contains(RoleNames.Admin) || currentUser.Roles.Contains(RoleNames.ComplianceManager))
        {
            return;
        }

        if (currentUser.Roles.Contains(RoleNames.Contributor) && occurrence.AssignedToUserId == currentUser.UserId)
        {
            return;
        }

        throw new AppValidationException("You do not have permission to update this task occurrence.");
    }

    private void AddAuditLog(ComplianceTaskOccurrence occurrence, string actionType, string description)
    {
        var currentUser = currentUserContextService.GetCurrentUser();
        dbContext.AuditLogEntries.Add(new AuditLogEntry
        {
            ComplianceTaskOccurrenceId = occurrence.Id,
            ActionType = actionType,
            Description = description,
            PerformedByUserId = string.IsNullOrWhiteSpace(currentUser.UserId) ? "system" : currentUser.UserId,
            PerformedByDisplayName = string.IsNullOrWhiteSpace(currentUser.DisplayName) ? "System" : currentUser.DisplayName
        });
    }

    private void ValidateUploadedFile(string fileName, long fileSizeBytes)
    {
        if (fileSizeBytes <= 0)
        {
            throw new AppValidationException("A non-empty file is required.");
        }

        if (fileSizeBytes > fileStorageOptions.MaxFileSizeBytes)
        {
            var maxMegabytes = fileStorageOptions.MaxFileSizeBytes / (1024 * 1024);
            throw new AppValidationException($"Files must be {maxMegabytes} MB or smaller.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !fileStorageOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new AppValidationException($"File type '{extension}' is not allowed.");
        }
    }

    private static readonly IReadOnlyDictionary<ComplianceTaskOccurrenceStatus, IReadOnlyCollection<ComplianceTaskOccurrenceStatus>> AllowedTransitions =
        new Dictionary<ComplianceTaskOccurrenceStatus, IReadOnlyCollection<ComplianceTaskOccurrenceStatus>>
        {
            [ComplianceTaskOccurrenceStatus.Draft] =
                [ComplianceTaskOccurrenceStatus.Pending, ComplianceTaskOccurrenceStatus.Cancelled],
            [ComplianceTaskOccurrenceStatus.Pending] =
                [ComplianceTaskOccurrenceStatus.InProgress, ComplianceTaskOccurrenceStatus.Completed, ComplianceTaskOccurrenceStatus.Cancelled],
            [ComplianceTaskOccurrenceStatus.InProgress] =
                [ComplianceTaskOccurrenceStatus.Pending, ComplianceTaskOccurrenceStatus.Completed, ComplianceTaskOccurrenceStatus.Cancelled],
            [ComplianceTaskOccurrenceStatus.Overdue] =
                [ComplianceTaskOccurrenceStatus.InProgress, ComplianceTaskOccurrenceStatus.Completed, ComplianceTaskOccurrenceStatus.Cancelled],
            [ComplianceTaskOccurrenceStatus.Completed] = Array.Empty<ComplianceTaskOccurrenceStatus>(),
            [ComplianceTaskOccurrenceStatus.Cancelled] = Array.Empty<ComplianceTaskOccurrenceStatus>()
        };
}
