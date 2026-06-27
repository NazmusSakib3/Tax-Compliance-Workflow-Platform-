using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxCompliance.Api.Filters;
using TaxCompliance.Application.Auth;

namespace TaxCompliance.Api.Controllers;

[ApiController]
[Route("api/authorization-demo")]
[Authorize]
[DevelopmentOnly]
public class AuthorizationDemoController : ControllerBase
{
    [HttpGet("reader")]
    [Authorize(Policy = AuthorizationPolicies.ReaderAccess)]
    public IActionResult Reader()
    {
        return Ok(new { message = "Reader access granted." });
    }

    [HttpGet("rules")]
    [Authorize(Policy = AuthorizationPolicies.RuleManagement)]
    public IActionResult Rules()
    {
        return Ok(new { message = "Rule management access granted." });
    }

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public IActionResult Admin()
    {
        return Ok(new { message = "Admin access granted." });
    }
}
