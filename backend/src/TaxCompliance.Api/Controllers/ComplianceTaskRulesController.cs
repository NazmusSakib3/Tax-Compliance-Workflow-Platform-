using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.Common;
using TaxCompliance.Application.ComplianceTaskRules;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Manages recurring compliance task rules.
/// </summary>
[ApiController]
[Route("api/compliance-task-rules")]
[Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
public class ComplianceTaskRulesController : ControllerBase
{
    private readonly IComplianceTaskRuleService complianceTaskRuleService;

    public ComplianceTaskRulesController(IComplianceTaskRuleService complianceTaskRuleService)
    {
        this.complianceTaskRuleService = complianceTaskRuleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ComplianceTaskRuleListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ComplianceTaskRuleListItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await complianceTaskRuleService.GetPagedAsync(new PagedListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, cancellationToken));
    }

    [HttpGet("{complianceTaskRuleId:guid}")]
    [ProducesResponseType(typeof(ComplianceTaskRuleDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceTaskRuleDetailDto>> GetById(Guid complianceTaskRuleId, CancellationToken cancellationToken)
    {
        return Ok(await complianceTaskRuleService.GetByIdAsync(complianceTaskRuleId, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(ComplianceTaskRuleDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ComplianceTaskRuleDetailDto>> Create([FromBody] SaveComplianceTaskRuleRequest request, CancellationToken cancellationToken)
    {
        var created = await complianceTaskRuleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { complianceTaskRuleId = created.Id }, created);
    }

    [HttpPut("{complianceTaskRuleId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(typeof(ComplianceTaskRuleDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceTaskRuleDetailDto>> Update(Guid complianceTaskRuleId, [FromBody] SaveComplianceTaskRuleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await complianceTaskRuleService.UpdateAsync(complianceTaskRuleId, request, cancellationToken));
    }

    [HttpDelete("{complianceTaskRuleId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid complianceTaskRuleId, CancellationToken cancellationToken)
    {
        await complianceTaskRuleService.DeleteAsync(complianceTaskRuleId, cancellationToken);
        return NoContent();
    }
}
