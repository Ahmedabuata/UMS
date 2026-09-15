using System.Security.Claims;
using System.Text.Json;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Services;
using Identity.Api.Validators;
using Identity.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _config;

    public AuthController(
        IdentityDbContext db,
        ITokenService tokenService,
        ILogger<AuthController> logger,
        IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
        _config = config;
    }

    private string Ip() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    private string UserAgent() => Request.Headers["User-Agent"].ToString() ?? "unknown";

    private async Task Audit(string action, Guid? uid, string? eid, object? oldV = null, object? newV = null)
    {
        var userIdClaim = User?.FindFirst("userId")?.Value;
        var createdBy = !string.IsNullOrEmpty(userIdClaim) ? Guid.Parse(userIdClaim) : uid;

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = uid,
            Action = action,
            Entity = "auth",
            EntityId = eid,
            TableName = "users",
            OldValues = oldV != null ? JsonSerializer.Serialize(oldV) : null,
            NewValues = newV != null ? JsonSerializer.Serialize(newV) : null,
            IpAddress = Ip(),
            UserAgent = UserAgent(),
            CreatedBy = createdBy,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task<UserDto> ToUserDtoAsync(User user)
    {
        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role != null ? ur.Role.RoleName : string.Empty)
            .Where(r => !string.IsNullOrEmpty(r))
            .ToListAsync();

        var perms = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p != null ? p.PermissionName : string.Empty)
            .Where(pn => !string.IsNullOrEmpty(pn))
            .Distinct()
            .ToListAsync();

        var groups = await _db.UserGroups
            .Where(ug => ug.UserId == user.Id)
            .Include(ug => ug.Group)
            .Select(ug => ug.Group != null ? ug.Group.Name : string.Empty)
            .Where(g => !string.IsNullOrEmpty(g))
            .ToListAsync();

        var profile = await _db.UserProfiles
            .Where(p => p.UserId == user.Id)
            .Select(p => new UserProfileDto(
                p.Id,
                p.UserId,
                p.Address,
                p.City,
                p.PostalCode,
                p.MaritalStatus,
                p.DateOfBirth,
                p.Gender,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.MustChangePassword,
            user.FailedLoginAttempts,
            user.LockoutEnd,
            user.FirstName,
            user.LastName,
            user.CreatedAt,
            user.UpdatedAt,
            roles,
            perms,
            groups,
            profile
        );
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var inputErr = GlobalValidators.ValidateUserInput(req.Email, req.Username, req.PhoneNumber);
            if (inputErr != null) return BadRequest(new { code = "INVALID_INPUT", message = inputErr });

            var passwordErr = GlobalValidators.ValidatePassword(req.Password);
            if (passwordErr != null) return BadRequest(new { code = "INVALID_PASSWORD", message = passwordErr });

            var emailNorm = GlobalValidators.NormalizeEmail(req.Email);
            var userNorm = GlobalValidators.NormalizeUsername(req.Username);

            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm.ToLower(), cancellationToken))
                return Conflict(new { code = "DUPLICATE_EMAIL", message = "Email already exists" });
            if (await _db.Users.AnyAsync(u => u.Username == userNorm, cancellationToken))
                return Conflict(new { code = "DUPLICATE_USERNAME", message = "Username already exists" });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = userNorm,
                Email = emailNorm,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                PhoneNumber = req.PhoneNumber?.Trim(),
                FirstName = req.FirstName?.Trim(),
                LastName = req.LastName?.Trim(),
                IsActive = true,
                MustChangePassword = false,
                FailedLoginAttempts = 0,
                LockoutEnd = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);

            var userRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "USER", cancellationToken);
            if (userRole != null)
            {
                _db.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = userRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await Audit("REGISTER", user.Id, user.Id.ToString(), null, new { user.Email, user.Username });

            var dto = await ToUserDtoAsync(user);
            var (accessToken, accessExp) = await _tokenService.GenerateAccessTokenAsync(user);
            var (refreshEntity, rawToken) = await _tokenService.GenerateRefreshTokenAsync(user.Id, Ip(), UserAgent());

            return Ok(new AuthResponse(accessToken, rawToken, accessExp, refreshEntity.ExpiresAt, dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred" });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.EmailOrUsername) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { code = "CREDENTIALS_REQUIRED", message = "EmailOrUsername and Password are required" });

            var norm = req.EmailOrUsername.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == norm || u.Username.ToLower() == norm, cancellationToken);
            if (user == null) return Unauthorized(new { code = "INVALID_CREDENTIALS", message = "Invalid credentials" });

            if (!user.IsActive) return StatusCode(403, new { code = "ACCOUNT_DEACTIVATED", message = "Account deactivated" });

            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
                return StatusCode(423, new { code = "ACCOUNT_LOCKED", message = $"Account locked until {user.LockoutEnd:O}" });

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await _db.SaveChangesAsync(cancellationToken);
                await Audit("LOGIN_FAILED", user.Id, user.Id.ToString(), null, new { Attempt = user.FailedLoginAttempts, Ip = Ip() });
                return Unauthorized(new { code = "INVALID_CREDENTIALS", message = "Invalid credentials" });
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _db.SaveChangesAsync(cancellationToken);

            var dto = await ToUserDtoAsync(user);
            var (accessToken, accessExp) = await _tokenService.GenerateAccessTokenAsync(user);
            var (refreshEntity, rawToken) = await _tokenService.GenerateRefreshTokenAsync(user.Id, Ip(), UserAgent());

            await Audit("LOGIN_SUCCESS", user.Id, user.Id.ToString(), null, new { Ip = Ip() });

            return Ok(new AuthResponse(accessToken, rawToken, accessExp, refreshEntity.ExpiresAt, dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred" });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req?.RefreshToken))
                return BadRequest(new { code = "REFRESH_TOKEN_REQUIRED", message = "Refresh token required" });

            var hashed = _tokenService.HashToken(req.RefreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hashed, cancellationToken);
            if (stored == null) return Unauthorized(new { code = "INVALID_REFRESH_TOKEN", message = "Invalid refresh token" });

            if (stored.Revoked)
            {
                await _tokenService.RevokeAllUserTokensAsync(stored.UserId, Ip());
                return Unauthorized(new { code = "TOKEN_REUSE_DETECTED", message = "Token reuse detected" });
            }

            if (stored.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new { code = "REFRESH_TOKEN_EXPIRED", message = "Refresh token expired" });

            var user = await _db.Users.FindAsync(new object[] { stored.UserId }, cancellationToken);
            if (user == null || !user.IsActive) return Unauthorized(new { code = "USER_INACTIVE", message = "User inactive" });

            var (newRefreshEntity, newRawToken) = await _tokenService.RotateRefreshTokenAsync(stored, Ip(), UserAgent());
            var (accessToken, accessExp) = await _tokenService.GenerateAccessTokenAsync(user);
            var dto = await ToUserDtoAsync(user);

            return Ok(new AuthResponse(accessToken, newRawToken, accessExp, newRefreshEntity.ExpiresAt, dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred" });
        }
    }

    [Authorize]
    [HttpPost("revoke-token")]
    [HttpPost("logout")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest? req, CancellationToken cancellationToken = default)
    {
        try
        {
            string? rawToken = req?.RefreshToken;
            if (string.IsNullOrWhiteSpace(rawToken) && Request.Headers.ContainsKey("X-Refresh-Token"))
                rawToken = Request.Headers["X-Refresh-Token"].ToString();

            if (string.IsNullOrWhiteSpace(rawToken))
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirst("userId")?.Value;
                    if (Guid.TryParse(uidStr, out var uid))
                    {
                        await _tokenService.RevokeAllUserTokensAsync(uid, Ip());
                        await Audit("LOGOUT_ALL", uid, uid.ToString());
                        return Ok(new { code = "SUCCESS", message = "All tokens revoked" });
                    }
                }
                return BadRequest(new { code = "REFRESH_TOKEN_REQUIRED", message = "Refresh token required" });
            }

            var hashed = _tokenService.HashToken(rawToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hashed, cancellationToken);
            if (stored != null)
            {
                stored.Revoked = true;
                stored.RevokedAt = DateTime.UtcNow;
                stored.RevokedByIp = Ip();
                await _db.SaveChangesAsync(cancellationToken);
                await Audit("LOGOUT", stored.UserId, stored.UserId.ToString());
            }

            return Ok(new { code = "SUCCESS", message = "Token revoked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation / logout");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred" });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirst("userId")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid token claims" });

            var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null) return NotFound(new { code = "USER_NOT_FOUND", message = "User not found" });

            var dto = await ToUserDtoAsync(user);
            return Ok(new MeResponse(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user profile");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred" });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User?.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { code = "UNAUTHORIZED", message = "Invalid or missing token claims." });

            var u = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (u == null || !u.IsActive)
                return NotFound(new { code = "USER_NOT_FOUND", message = "User not found or inactive." });

            if (string.IsNullOrEmpty(u.PasswordHash) || !BCrypt.Net.BCrypt.Verify(req.CurrentPassword, u.PasswordHash))
                return BadRequest(new { code = "INVALID_CURRENT_PASSWORD", message = "Incorrect current password." });

            if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8)
                return BadRequest(new { code = "WEAK_PASSWORD", message = "New password must be at least 8 characters long." });

            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            u.MustChangePassword = false;
            u.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _tokenService.RevokeAllUserTokensAsync(userId, remoteIp, userId.ToString());

            await Audit("CHANGE_PASSWORD", userId, userId.ToString(), null, new { u.Email });

            return Ok(new { code = "SUCCESS", message = "Password changed successfully. Please log in with your new password." });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The change password request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while changing password for user.");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }
}