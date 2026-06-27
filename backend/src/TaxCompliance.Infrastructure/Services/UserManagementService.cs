using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Users;
using TaxCompliance.Infrastructure.Identity;
using TaxCompliance.Infrastructure.Persistence;

namespace TaxCompliance.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IOrganizationScopeService organizationScope;
    private readonly ICurrentUserContextService currentUserContextService;

    public UserManagementService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOrganizationScopeService organizationScope,
        ICurrentUserContextService currentUserContextService)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.organizationScope = organizationScope;
        this.currentUserContextService = currentUserContextService;
    }

    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PaginationHelper.Normalize(query.Page, query.PageSize);
        var userQuery = userManager.Users.AsQueryable();

        if (organizationScope.GetOrganizationId() is Guid organizationId)
        {
            userQuery = userQuery.Where(user => user.OrganizationId == organizationId);
        }
        else if (!OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser()))
        {
            organizationScope.RequireOrganizationId();
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            userQuery = userQuery.Where(user =>
                user.DisplayName.Contains(search) ||
                (user.Email != null && user.Email.Contains(search)));
        }

        var totalCount = await userQuery.CountAsync(cancellationToken);
        var users = await userQuery
            .OrderBy(user => user.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rolesByUserId = await GetRolesByUserIdsAsync(
            users.Select(user => user.Id).ToArray(),
            cancellationToken);

        var results = users
            .Select(user => MapUser(user, rolesByUserId.GetValueOrDefault(user.Id, [])))
            .ToList();

        return PaginationHelper.Create(results, page, pageSize, totalCount);
    }

    public async Task<UserListItemDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        ValidateRole(request.Role);

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            throw new AppValidationException("A user with this email already exists.");
        }

        var organizationId = organizationScope.GetOrganizationId();
        if (!organizationId.HasValue && !OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser()))
        {
            throw new AppValidationException("Your account is not assigned to an organization.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = true,
            LockoutEnabled = true,
            OrganizationId = organizationId
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new AppValidationException("Unable to create user.", ToErrorDictionary(createResult));
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new AppValidationException("Unable to assign the requested role.", ToErrorDictionary(roleResult));
        }

        return await MapUserAsync(user, cancellationToken);
    }

    public async Task<UserListItemDto> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        ValidateRole(request.Role);

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException("User was not found.");

        EnsureUserAccess(user);

        user.DisplayName = request.DisplayName;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new AppValidationException("Unable to update user.", ToErrorDictionary(updateResult));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(request.Role))
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    throw new AppValidationException("Unable to update user role.", ToErrorDictionary(removeResult));
                }
            }

            var addResult = await userManager.AddToRoleAsync(user, request.Role);
            if (!addResult.Succeeded)
            {
                throw new AppValidationException("Unable to assign the requested role.", ToErrorDictionary(addResult));
            }
        }

        if (request.IsActive)
        {
            await userManager.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }

        return await MapUserAsync(user, cancellationToken);
    }

    private void EnsureUserAccess(ApplicationUser user)
    {
        if (OrganizationQueryExtensions.IsPlatformAdmin(currentUserContextService.GetCurrentUser())
            && !organizationScope.HasOrganizationScope())
        {
            return;
        }

        if (!user.OrganizationId.HasValue)
        {
            throw new EntityNotFoundException("User was not found.");
        }

        organizationScope.EnsureSameOrganization(user.OrganizationId.Value);
    }

    private async Task<Dictionary<string, string[]>> GetRolesByUserIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var roleAssignments = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, RoleName = role.Name }
        ).ToListAsync(cancellationToken);

        return roleAssignments
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(assignment => assignment.RoleName!).ToArray());
    }

    private async Task<UserListItemDto> MapUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return MapUser(user, roles);
    }

    private static UserListItemDto MapUser(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        return new UserListItemDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToArray(),
            IsActive = user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow
        };
    }

    private static void ValidateRole(string role)
    {
        if (!RoleNames.All.Contains(role))
        {
            throw new AppValidationException($"Role '{role}' is not supported.");
        }
    }

    private static IDictionary<string, string[]> ToErrorDictionary(IdentityResult result)
    {
        return new Dictionary<string, string[]>
        {
            ["identity"] = result.Errors.Select(error => error.Description).ToArray()
        };
    }
}
