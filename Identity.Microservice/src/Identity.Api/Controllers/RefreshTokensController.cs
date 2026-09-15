using System.Security.Claims;
using Identity.Api.DTOs;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Api.Interfaces;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/refresh-tokens")]
[Authorize]
public class RefreshTokensController : ControllerBase
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<RefreshTokensController> _logger;

    public RefreshTokensController(
        IRefreshTokenService refreshTokenService,
        ILogger<RefreshTokensController> logger)
    {
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves paginated refresh tokens for the current authenticated user.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyTokens(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var (tokens, total) = await _refreshTokenService.GetUserTokensPaginatedAsync(userId, page, pageSize, cancellationToken);

        return Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Data = tokens
        });
    }

    /// <summary>
    /// Revokes a specific refresh token by its raw value for the current user.
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken(
        [FromBody] RevokeTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Token is required." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var success = await _refreshTokenService.RevokeTokenAsync(request.RefreshToken, ipAddress, cancellationToken);

        if (!success)
            return NotFound(new { message = "Active token not found." });

        return Ok(new { message = "Token revoked successfully." });
    }

    /// <summary>
    /// Revokes all active refresh tokens for the current authenticated user.
    /// </summary>
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAllMyTokens(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _refreshTokenService.RevokeAllUserTokensAsync(userId, ipAddress, userId.ToString(), cancellationToken);

        return Ok(new { message = "All user tokens revoked successfully." });
    }
}