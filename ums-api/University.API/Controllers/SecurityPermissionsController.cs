using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.API.Controllers;

[ApiController]
[Route("api/security/permissions")]
[HasPermission("SECURITY_PERMISSION_READ")]
public class SecurityPermissionsController : ControllerBase
{
    private readonly ISecurityPermissionQueryService _permissionService;

    public SecurityPermissionsController(ISecurityPermissionQueryService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? module = null)
    {
        var result = await _permissionService.GetAllAsync(module);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("grouped")]
    public async Task<IActionResult> GetGrouped()
    {
        var result = await _permissionService.GetGroupedByModuleAsync();
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules()
    {
        var result = await _permissionService.GetModulesAsync();
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
