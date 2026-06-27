using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTemplates;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Manages reusable compliance templates.
/// </summary>
[ApiController]
[Route("api/compliance-templates")]
[Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
public class ComplianceTemplatesController : ControllerBase
{
    private readonly IComplianceTemplateService complianceTemplateService;

    public ComplianceTemplatesController(IComplianceTemplateService complianceTemplateService)
    {
        this.complianceTemplateService = complianceTemplateService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ComplianceTemplateListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ComplianceTemplateListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await complianceTemplateService.GetPagedAsync(new PagedListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, cancellationToken));
    }

    [HttpGet("{complianceTemplateId:guid}")]
    [ProducesResponseType(typeof(ComplianceTemplateDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceTemplateDetailDto>> GetById(Guid complianceTemplateId, CancellationToken cancellationToken)
    {
        return Ok(await complianceTemplateService.GetByIdAsync(complianceTemplateId, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(ComplianceTemplateDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ComplianceTemplateDetailDto>> Create([FromBody] SaveComplianceTemplateRequest request, CancellationToken cancellationToken)
    {
        var created = await complianceTemplateService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { complianceTemplateId = created.Id }, created);
    }

    [HttpPut("{complianceTemplateId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(ComplianceTemplateDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceTemplateDetailDto>> Update(Guid complianceTemplateId, [FromBody] SaveComplianceTemplateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await complianceTemplateService.UpdateAsync(complianceTemplateId, request, cancellationToken));
    }

    [HttpDelete("{complianceTemplateId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid complianceTemplateId, CancellationToken cancellationToken)
    {
        await complianceTemplateService.DeleteAsync(complianceTemplateId, cancellationToken);
        return NoContent();
    }
}

