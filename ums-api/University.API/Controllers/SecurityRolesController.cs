using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.API.Controllers;

[ApiController]
[Route("api/security/roles")]
[HasPermission("SECURITY_ROLE_READ")]
public class SecurityRolesController : ControllerBase
{
    private readonly ISecurityRoleService _roleService;

    public SecurityRolesController(ISecurityRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? branchCode = null)
    {
        var result = await _roleService.GetAllAsync(branchCode);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _roleService.GetByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission("SECURITY_ROLE_WRITE")]
    public async Task<IActionResult> Create([FromBody] CreateSecurityRoleDto dto)
    {
        var result = await _roleService.CreateAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("SECURITY_ROLE_WRITE")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSecurityRoleDto dto)
    {
        var result = await _roleService.UpdateAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [SuperAdminOnly]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roleService.DeleteAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> GetPermissionIds(Guid id)
    {
        var result = await _roleService.GetPermissionIdsAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission("SECURITY_ROLE_PERMISSIONS")]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] AssignPermissionsDto dto)
    {
        var result = await _roleService.SetPermissionsAsync(id, dto);
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
