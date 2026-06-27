using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Users;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Admin-only user and role management.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        this.userManagementService = userManagementService;
    }

    /// <summary>
    /// Returns all platform users with their assigned roles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await userManagementService.GetPagedAsync(new PagedListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, cancellationToken));
    }

    /// <summary>
    /// Creates a new user and assigns a single role.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserListItemDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await userManagementService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { userId = created.UserId }, created);
    }

    /// <summary>
    /// Updates display name, role, and active status for an existing user.
    /// </summary>
    [HttpPut("{userId}")]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserListItemDto>> Update(string userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await userManagementService.UpdateAsync(userId, request, cancellationToken));
    }
}
