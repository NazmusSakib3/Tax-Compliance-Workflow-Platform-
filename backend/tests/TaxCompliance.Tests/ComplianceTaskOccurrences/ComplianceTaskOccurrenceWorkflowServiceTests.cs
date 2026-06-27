using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskOccurrences;
using TaxCompliance.Application.FileStorage;
using TaxCompliance.Domain.Entities;
using TaxCompliance.Domain.Enums;
using TaxCompliance.Infrastructure.FileStorage;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Persistence;
using TaxCompliance.Infrastructure.Services;
using TaxCompliance.Tests.TestDoubles;

namespace TaxCompliance.Tests.ComplianceTaskOccurrences;

public class ComplianceTaskOccurrenceWorkflowServiceTests
{
    [Fact]
    public async Task ChangeStatusAsync_ShouldWriteAuditLogEntry()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "user-1");
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "user-1",
                DisplayName = "Assigned Contributor",
                Roles = [RoleNames.Contributor]
            }
        };

        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId },
            BuildFileStorageService(),
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var result = await service.ChangeStatusAsync(
            occurrence.Id,
            new UpdateTaskOccurrenceStatusRequest { Status = ComplianceTaskOccurrenceStatus.InProgress },
            CancellationToken.None);

        result.Status.Should().Be(ComplianceTaskOccurrenceStatus.InProgress);
        dbContext.AuditLogEntries.Should().ContainSingle(entry => entry.ActionType == "StatusChanged");
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldRejectContributorWhoIsNotAssigned()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "assigned-user");
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "different-user",
                DisplayName = "Other Contributor",
                Roles = [RoleNames.Contributor]
            }
        };

        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId },
            BuildFileStorageService(),
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var action = async () => await service.ChangeStatusAsync(
            occurrence.Id,
            new UpdateTaskOccurrenceStatusRequest { Status = ComplianceTaskOccurrenceStatus.InProgress },
            CancellationToken.None);

        await action.Should().ThrowAsync<AppValidationException>()
            .WithMessage("*do not have permission*");
    }

    [Fact]
    public async Task AssignAsync_ShouldAllowComplianceManagerAndWriteAssignmentAuditEntry()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "user-1");
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "manager-1",
                DisplayName = "Compliance Manager",
                Roles = [RoleNames.ComplianceManager]
            }
        };

        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId },
            BuildFileStorageService(),
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var result = await service.AssignAsync(
            occurrence.Id,
            new UpdateTaskOccurrenceAssignmentRequest { AssignedToUserId = "user-2" },
            CancellationToken.None);

        result.AssignedToUserId.Should().Be("user-2");
        dbContext.AuditLogEntries.Should().Contain(entry =>
            entry.ActionType == "AssignmentChanged" &&
            entry.PerformedByUserId == "manager-1");
    }

    [Fact]
    public async Task AssignAsync_ShouldRejectAssigneeFromDifferentOrganization()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "user-1");
        var occurrenceOrganizationId = occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId;
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "manager-1",
                DisplayName = "Compliance Manager",
                Roles = [RoleNames.ComplianceManager]
            }
        };

        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(occurrenceOrganizationId, Guid.NewGuid()),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = occurrenceOrganizationId },
            BuildFileStorageService(),
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var action = async () => await service.AssignAsync(
            occurrence.Id,
            new UpdateTaskOccurrenceAssignmentRequest { AssignedToUserId = "user-2" },
            CancellationToken.None);

        await action.Should().ThrowAsync<AppValidationException>()
            .WithMessage("*Assigned user is invalid*");
    }

    [Fact]
    public async Task DownloadDocumentAsync_ShouldRejectUnassignedContributorBeforeOpeningStorage()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "assigned-user");
        var document = new TaskDocument
        {
            Id = Guid.NewGuid(),
            ComplianceTaskOccurrenceId = occurrence.Id,
            FileName = "vat-return.pdf",
            StoredPath = "stored-file.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 128,
            UploadedByUserId = "assigned-user",
            UploadedByDisplayName = "Assigned User"
        };
        dbContext.TaskDocuments.Add(document);
        await dbContext.SaveChangesAsync();
        var fileStorage = new Mock<IFileStorageService>();
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "different-user",
                DisplayName = "Other Contributor",
                Roles = [RoleNames.Contributor]
            }
        };
        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId },
            fileStorage.Object,
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var action = async () => await service.DownloadDocumentAsync(document.Id, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
        fileStorage.Verify(
            storage => storage.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DownloadDocumentAsync_ShouldRejectCrossOrganizationUserBeforeOpeningStorage()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "assigned-user");
        var document = new TaskDocument
        {
            Id = Guid.NewGuid(),
            ComplianceTaskOccurrenceId = occurrence.Id,
            FileName = "vat-return.pdf",
            StoredPath = "stored-file.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 128,
            UploadedByUserId = "assigned-user",
            UploadedByDisplayName = "Assigned User"
        };
        dbContext.TaskDocuments.Add(document);
        await dbContext.SaveChangesAsync();
        var fileStorage = new Mock<IFileStorageService>();
        var otherOrganizationId = Guid.NewGuid();
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "manager-1",
                DisplayName = "Other Org Manager",
                Roles = [RoleNames.ComplianceManager]
            }
        };
        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = otherOrganizationId },
            fileStorage.Object,
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var action = async () => await service.DownloadDocumentAsync(document.Id, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
        fileStorage.Verify(
            storage => storage.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDocumentsAsync_ShouldRejectUnassignedContributor()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "assigned-user");
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "different-user",
                DisplayName = "Other Contributor",
                Roles = [RoleNames.Contributor]
            }
        };
        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = occurrence.ComplianceTaskRule!.LegalEntity!.OrganizationId },
            BuildFileStorageService(),
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var action = async () => await service.GetDocumentsAsync(occurrence.Id, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetCommentsAsync_ShouldRejectCrossOrganizationUser()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "assigned-user");
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "manager-1",
                DisplayName = "Other Org Manager",
                Roles = [RoleNames.ComplianceManager]
            }
        };
        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = Guid.NewGuid() },
            BuildFileStorageService(),
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var action = async () => await service.GetCommentsAsync(occurrence.Id, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetAuditLogAsync_ShouldRejectCrossOrganizationUser()
    {
        await using var dbContext = BuildDbContext();
        var occurrence = SeedOccurrence(dbContext, assignedToUserId: "assigned-user");
        var currentUser = new FakeCurrentUserContextService
        {
            CurrentUser = new CurrentUserContext
            {
                UserId = "manager-1",
                DisplayName = "Other Org Manager",
                Roles = [RoleNames.ComplianceManager]
            }
        };
        var service = new ComplianceTaskOccurrenceWorkflowService(
            dbContext,
            BuildUserManager(),
            currentUser,
            new FakeOrganizationScopeService { OrganizationId = Guid.NewGuid() },
            BuildFileStorageService(),
            new FakeDashboardCacheInvalidationService(),
            BuildFileStorageOptions());

        var action = async () => await service.GetAuditLogAsync(occurrence.Id, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    private static ApplicationDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ComplianceTaskOccurrence SeedOccurrence(ApplicationDbContext dbContext, string assignedToUserId)
    {
        var legalEntity = new LegalEntity { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Name = "Northwind GmbH", RegistrationNumber = "REG-1", TaxIdentifier = "TAX-1" };
        var jurisdiction = new Jurisdiction { Id = Guid.NewGuid(), OrganizationId = legalEntity.OrganizationId, Name = "Germany", CountryCode = "DE", RegionCode = "BE", FilingAuthority = "Berlin Tax Office" };
        var template = new ComplianceTemplate { Id = Guid.NewGuid(), OrganizationId = legalEntity.OrganizationId, Name = "VAT", FilingType = "VAT Return" };
        var rule = new ComplianceTaskRule
        {
            Id = Guid.NewGuid(),
            LegalEntityId = legalEntity.Id,
            JurisdictionId = jurisdiction.Id,
            ComplianceTemplateId = template.Id,
            Title = "Monthly VAT",
            RecurrenceType = RecurrenceType.Monthly,
            DueDayOfMonth = 10,
            LegalEntity = legalEntity,
            Jurisdiction = jurisdiction,
            ComplianceTemplate = template
        };
        var occurrence = new ComplianceTaskOccurrence
        {
            Id = Guid.NewGuid(),
            ComplianceTaskRuleId = rule.Id,
            ComplianceTaskRule = rule,
            AssignedToUserId = assignedToUserId,
            PeriodStartDate = new DateOnly(2026, 4, 1),
            PeriodEndDate = new DateOnly(2026, 4, 30),
            DueDate = new DateOnly(2026, 4, 10),
            Status = ComplianceTaskOccurrenceStatus.Pending
        };

        dbContext.LegalEntities.Add(legalEntity);
        dbContext.Jurisdictions.Add(jurisdiction);
        dbContext.ComplianceTemplates.Add(template);
        dbContext.ComplianceTaskRules.Add(rule);
        dbContext.ComplianceTaskOccurrences.Add(occurrence);
        dbContext.SaveChanges();
        return occurrence;
    }

    private static UserManager<ApplicationUser> BuildUserManager(Guid? organizationId = null, Guid? assigneeOrganizationId = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new Mock<ILogger<UserManager<ApplicationUser>>>().Object);

        userManager.Setup(manager => manager.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) => new ApplicationUser
            {
                Id = userId,
                OrganizationId = userId == "user-2"
                    ? assigneeOrganizationId ?? organizationId
                    : organizationId,
                DisplayName = userId == "user-2" ? "Second Assignee" : "Assigned Contributor",
                Email = userId == "user-2" ? "second@example.com" : "assigned@example.com"
            });

        return userManager.Object;
    }

    private static IFileStorageService BuildFileStorageService()
    {
        var fileStorage = new Mock<IFileStorageService>();
        fileStorage.Setup(service => service.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-file.bin");
        fileStorage.Setup(service => service.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());
        return fileStorage.Object;
    }

    private static IOptions<LocalFileStorageOptions> BuildFileStorageOptions()
    {
        return Options.Create(new LocalFileStorageOptions());
    }
}
