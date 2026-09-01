using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using University.Core.Interfaces.Services;
using University.Shared.DTOs.Auth;

namespace University.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto.Email, dto.Password);
        if (result.IsFailure)
        {
            return ToErrorResult(result);
        }
        return Ok(result.Value);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (result.IsFailure)
        {
            return ToErrorResult(result);
        }
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var result = await _authService.RefreshTokenAsync(refreshToken);
        if (result.IsFailure)
        {
            return ToErrorResult(result);
        }
        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(Guid userId, string oldPassword, string newPassword)
    {
        var result = await _authService.ChangePasswordAsync(userId, oldPassword, newPassword);
        return result.IsFailure ? ToErrorResult<bool>(result) : Ok(result.Value);
    }

    private IActionResult ToErrorResult<T>(University.Shared.Common.Result<T> result)
    {
        if (result.Error is null)
        {
            return BadRequest();
        }
        return result.Error.Type switch
        {
            University.Shared.Common.ErrorType.NotFound => NotFound(result.Error),
            University.Shared.Common.ErrorType.Unauthorized => Unauthorized(result.Error),
            University.Shared.Common.ErrorType.Forbidden => Forbid(),
            _ => BadRequest(result.Error)
        };
    }
}
