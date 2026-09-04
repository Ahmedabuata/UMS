using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;
using University.Shared.Configuration;

namespace University.API.Controllers;

[ApiController]
[Route("api/security/policies")]
[HasPermission("SECURITY_POLICY_READ")]
public class SecurityPoliciesController : ControllerBase
{
    private readonly ISecurityPolicyService _policyService;

    public SecurityPoliciesController(ISecurityPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPolicy()
    {
        var result = await _policyService.GetAsync();
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut]
    [HasPermission("SECURITY_POLICY_WRITE")]
    public async Task<IActionResult> UpdatePolicy([FromBody] SecuritySettings settings)
    {
        var result = await _policyService.UpdateAsync(settings);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    private IActionResult ErrorResult<T>(Result<T> result)
    {
        if (result.Error is null)
        {
            return BadRequest();
        }
        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Unauthorized => Unauthorized(result.Error),
            ErrorType.Forbidden => Forbid(),
            _ => BadRequest(result.Error)
        };
    }
}
