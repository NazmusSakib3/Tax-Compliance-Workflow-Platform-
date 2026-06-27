using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Application.Auth;
using TaxCompliance.Application.ComplianceTaskOccurrences;

namespace TaxCompliance.Api.Controllers;

/// <summary>
/// Development endpoints for recurring task occurrence generation.
/// </summary>
[ApiController]
[Route("api/compliance-task-occurrences")]
[Authorize(Policy = AuthorizationPolicies.RuleManagement)]
public class ComplianceTaskOccurrencesController : ControllerBase
{
    private readonly IComplianceTaskOccurrenceGenerationService generationService;

    public ComplianceTaskOccurrencesController(IComplianceTaskOccurrenceGenerationService generationService)
    {
        this.generationService = generationService;
    }

    /// <summary>
    /// Triggers occurrence generation immediately.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(OccurrenceGenerationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OccurrenceGenerationResultDto>> Generate(CancellationToken cancellationToken)
    {
        var result = await generationService.GenerateAsync(cancellationToken);
        return Ok(result);
    }
}
