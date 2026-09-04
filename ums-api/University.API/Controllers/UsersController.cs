using Microsoft.AspNetCore.Mvc;
using University.API.Attributes;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.API.Controllers;

[ApiController]
[Route("api/users")]
[HasPermission("USER_READ")]
public class UsersController : ControllerBase
{
    private readonly ISecurityUserService _userService;

    public UsersController(ISecurityUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search = null,
        [FromQuery] bool? active = null,
        [FromQuery] bool? mustChangePwd = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _userService.SearchAsync(search, null, null, active, page, pageSize, mustChangePwd);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost("create-for-employee/{employeeId:guid}")]
    [HasPermission("USER_WRITE")]
    public async Task<IActionResult> CreateForEmployee(Guid employeeId, [FromBody] CreateUserForEmployeeDto dto)
    {
        var result = await _userService.CreateForEmployeeAsync(employeeId, dto);
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