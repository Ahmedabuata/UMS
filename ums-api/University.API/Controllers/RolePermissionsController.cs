using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.API.Controllers;

[ApiController]
[Route("api/security/roles/{roleId:guid}/permissions")]
[HasPermission("SECURITY_ROLE_PERMISSIONS")]
public class RolePermissionsController : ControllerBase
{
    private readonly IRolePermissionService _permissionService;

    public RolePermissionsController(IRolePermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpPost("{permissionId:guid}")]
    public async Task<IActionResult> Assign(Guid roleId, Guid permissionId)
    {
        var result = await _permissionService.AssignPermissionToRoleAsync(roleId, permissionId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{permissionId:guid}")]
    [SuperAdminOnly]
    public async Task<IActionResult> Remove(Guid roleId, Guid permissionId)
    {
        var result = await _permissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
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
