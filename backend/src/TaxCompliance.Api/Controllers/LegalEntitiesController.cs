using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.LegalEntities;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Manages legal entities that belong to organizations.
/// </summary>
[ApiController]
[Route("api/legal-entities")]
[Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
public class LegalEntitiesController : ControllerBase
{
    private readonly ILegalEntityService legalEntityService;

    public LegalEntitiesController(ILegalEntityService legalEntityService)
    {
        this.legalEntityService = legalEntityService;
    }

    /// <summary>
    /// Returns all legal entities.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LegalEntityListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LegalEntityListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await legalEntityService.GetPagedAsync(new PagedListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, cancellationToken));
    }

    /// <summary>
    /// Returns a single legal entity by identifier.
    /// </summary>
    [HttpGet("{legalEntityId:guid}")]
    [ProducesResponseType(typeof(LegalEntityDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LegalEntityDetailDto>> GetById(Guid legalEntityId, CancellationToken cancellationToken)
    {
        return Ok(await legalEntityService.GetByIdAsync(legalEntityId, cancellationToken));
    }

    /// <summary>
    /// Creates a legal entity.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(LegalEntityDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LegalEntityDetailDto>> Create([FromBody] SaveLegalEntityRequest request, CancellationToken cancellationToken)
    {
        var created = await legalEntityService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { legalEntityId = created.Id }, created);
    }

    /// <summary>
    /// Updates a legal entity.
    /// </summary>
    [HttpPut("{legalEntityId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(LegalEntityDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LegalEntityDetailDto>> Update(Guid legalEntityId, [FromBody] SaveLegalEntityRequest request, CancellationToken cancellationToken)
    {
        return Ok(await legalEntityService.UpdateAsync(legalEntityId, request, cancellationToken));
    }

    /// <summary>
    /// Deletes a legal entity.
    /// </summary>
    [HttpDelete("{legalEntityId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid legalEntityId, CancellationToken cancellationToken)
    {
        await legalEntityService.DeleteAsync(legalEntityId, cancellationToken);
        return NoContent();
    }
}

