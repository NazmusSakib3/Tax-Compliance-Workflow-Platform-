using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Organizations;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Manages top-level organizations.
/// </summary>
[ApiController]
[Route("api/organizations")]
[Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        this.organizationService = organizationService;
    }

    /// <summary>
    /// Returns paginated organizations.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrganizationListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrganizationListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await organizationService.GetPagedAsync(new PagedListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, cancellationToken));
    }

    /// <summary>
    /// Returns a single organization by identifier.
    /// </summary>
    [HttpGet("{organizationId:guid}")]
    [ProducesResponseType(typeof(OrganizationDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDetailDto>> GetById(Guid organizationId, CancellationToken cancellationToken)
    {
        return Ok(await organizationService.GetByIdAsync(organizationId, cancellationToken));
    }

    /// <summary>
    /// Creates a new organization.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(OrganizationDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationDetailDto>> Create([FromBody] SaveOrganizationRequest request, CancellationToken cancellationToken)
    {
        var created = await organizationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { organizationId = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing organization.
    /// </summary>
    [HttpPut("{organizationId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(OrganizationDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDetailDto>> Update(Guid organizationId, [FromBody] SaveOrganizationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await organizationService.UpdateAsync(organizationId, request, cancellationToken));
    }

    /// <summary>
    /// Deletes an organization.
    /// </summary>
    [HttpDelete("{organizationId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid organizationId, CancellationToken cancellationToken)
    {
        await organizationService.DeleteAsync(organizationId, cancellationToken);
        return NoContent();
    }
}

