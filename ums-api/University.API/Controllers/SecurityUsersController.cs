using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.API.Controllers;

[ApiController]
[Route("api/security/users")]
[HasPermission("SECURITY_USER_READ")]
public class SecurityUsersController : ControllerBase
{
    private readonly ISecurityUserService _userService;

    public SecurityUsersController(ISecurityUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search = null,
        [FromQuery] string? branchCode = null,
        [FromQuery] string? userType = null,
        [FromQuery] bool? active = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _userService.SearchAsync(search, branchCode, userType, active, page, pageSize, null);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission("SECURITY_USER_WRITE")]
    public async Task<IActionResult> Create([FromBody] CreateSecurityUserDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("SECURITY_USER_WRITE")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSecurityUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [SuperAdminOnly]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _userService.DeleteAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/activate")]
    [HasPermission("SECURITY_USER_WRITE")]
    public async Task<IActionResult> Activate(Guid id, [FromQuery] bool activate = true)
    {
        var result = await _userService.ActivateAsync(id, activate);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/unlock")]
    [HasPermission("SECURITY_USER_UNLOCK")]
    public async Task<IActionResult> Unlock(Guid id)
    {
        var result = await _userService.UnlockAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/reset-password")]
    [HasPermission("SECURITY_USER_UNLOCK")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequestDto dto)
    {
        var result = await _userService.ResetPasswordAsync(id, dto.NewPassword);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}/roles")]
    [HasPermission("SECURITY_USER_WRITE")]
    public async Task<IActionResult> SetRoles(Guid id, [FromBody] SetUserRolesDto dto)
    {
        var result = await _userService.SetRolesAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/roles")]
    public async Task<IActionResult> GetRoles(Guid id)
    {
        var result = await _userService.GetUserRolesAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/groups")]
    public async Task<IActionResult> GetGroups(Guid id)
    {
        var result = await _userService.GetUserGroupsAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/groups/{groupId:guid}")]
    [HasPermission("SECURITY_GROUP_WRITE")]
    public async Task<IActionResult> AddToGroup(Guid id, Guid groupId)
    {
        var result = await _userService.AddToGroupAsync(id, groupId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}/groups/{groupId:guid}")]
    [HasPermission("SECURITY_GROUP_WRITE")]
    public async Task<IActionResult> RemoveFromGroup(Guid id, Guid groupId)
    {
        var result = await _userService.RemoveFromGroupAsync(id, groupId);
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
	
using var tx = _context.Database.BeginTransaction(_capBus, autoCommit: false);
var user = await _userService.CreateAsync(dto);
await _capBus.PublishAsync("ums.user.created", new UserCreatedEvent {
  ExternalUserId = user.Id, Email = user.Email, FullName = user.FullName
});
await tx.CommitAsync();	
	
	
}



