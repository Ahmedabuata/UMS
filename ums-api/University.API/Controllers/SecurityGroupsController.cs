using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.API.Controllers;

[ApiController]
[Route("api/security/groups")]
[HasPermission("SECURITY_GROUP_READ")]
public class SecurityGroupsController : ControllerBase
{
    private readonly ISecurityGroupService _groupService;

    public SecurityGroupsController(ISecurityGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? branchCode = null)
    {
        var result = await _groupService.GetAllAsync(branchCode);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _groupService.GetByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission("SECURITY_GROUP_WRITE")]
    public async Task<IActionResult> Create([FromBody] CreateSecurityGroupDto dto)
    {
        var result = await _groupService.CreateAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("SECURITY_GROUP_WRITE")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSecurityGroupDto dto)
    {
        var result = await _groupService.UpdateAsync(id, dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [SuperAdminOnly]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _groupService.DeleteAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var result = await _groupService.GetMembersAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/members/{userId:guid}")]
    [HasPermission("SECURITY_GROUP_WRITE")]
    public async Task<IActionResult> AddMember(Guid id, Guid userId)
    {
        var result = await _groupService.AddMemberAsync(id, userId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [HasPermission("SECURITY_GROUP_WRITE")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var result = await _groupService.RemoveMemberAsync(id, userId);
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
