using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.Jurisdictions;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Manages tax jurisdictions and filing authorities.
/// </summary>
[ApiController]
[Route("api/jurisdictions")]
[Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
public class JurisdictionsController : ControllerBase
{
    private readonly IJurisdictionService jurisdictionService;

    public JurisdictionsController(IJurisdictionService jurisdictionService)
    {
        this.jurisdictionService = jurisdictionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<JurisdictionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<JurisdictionListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await jurisdictionService.GetPagedAsync(new PagedListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, cancellationToken));
    }

    [HttpGet("{jurisdictionId:guid}")]
    [ProducesResponseType(typeof(JurisdictionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JurisdictionDetailDto>> GetById(Guid jurisdictionId, CancellationToken cancellationToken)
    {
        return Ok(await jurisdictionService.GetByIdAsync(jurisdictionId, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(JurisdictionDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<JurisdictionDetailDto>> Create([FromBody] SaveJurisdictionRequest request, CancellationToken cancellationToken)
    {
        var created = await jurisdictionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { jurisdictionId = created.Id }, created);
    }

    [HttpPut("{jurisdictionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(JurisdictionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JurisdictionDetailDto>> Update(Guid jurisdictionId, [FromBody] SaveJurisdictionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await jurisdictionService.UpdateAsync(jurisdictionId, request, cancellationToken));
    }

    [HttpDelete("{jurisdictionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid jurisdictionId, CancellationToken cancellationToken)
    {
        await jurisdictionService.DeleteAsync(jurisdictionId, cancellationToken);
        return NoContent();
    }
}

