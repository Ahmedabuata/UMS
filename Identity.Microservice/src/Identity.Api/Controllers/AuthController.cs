using System.Security.Claims;
using System.Text.Json;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Services;
using Identity.Api.Validators;
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
    private readonly IConfiguration _config;

    public AuthController(IdentityDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }

    private string Ip() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    private string UserAgent() => Request.Headers["User-Agent"].ToString() ?? "unknown";

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

        return new UserDto(user.Id, user.Username, user.Email, user.PhoneNumber, user.IsActive, user.MustChangePassword, user.CreatedAt, roles, perms, groups);
    }

    private async Task AuditAsync(string action, Guid? userId, string? entityId = null, object? oldVal = null, object? newVal = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityName = "users",
            EntityId = entityId,
            OldValues = oldVal != null ? JsonSerializer.Serialize(oldVal) : null,
            NewValues = newVal != null ? JsonSerializer.Serialize(newVal) : null,
            IpAddress = Ip(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        // 1. التحقق من صيغة البريد، اسم المستخدم، ورقم الهاتف
        var inputErr = GlobalValidators.ValidateUserInput(req.Email, req.Username, req.PhoneNumber);
        if (inputErr != null) return BadRequest(new { message = inputErr });

        // 2. التحقق من كلمة السر
        var passwordErr = GlobalValidators.ValidatePassword(req.Password);
        if (passwordErr != null) return BadRequest(new { message = passwordErr });

        var emailNorm = GlobalValidators.NormalizeEmail(req.Email);
        var userNorm = GlobalValidators.NormalizeUsername(req.Username);

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm.ToLower()))
            return Conflict(new { message = "Email already exists" });
        if (await _db.Users.AnyAsync(u => u.Username == userNorm))
            return Conflict(new { message = "Username already exists" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = userNorm,
            Email = emailNorm,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            PhoneNumber = req.PhoneNumber?.Trim(),
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        var userRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "USER");
        if (userRole != null)
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });

        await _db.SaveChangesAsync();
        await AuditAsync("REGISTER", user.Id, user.Id.ToString(), null, new { user.Email, user.Username });

        var dto = await ToUserDtoAsync(user);
        var (accessToken, accessExp) = await _tokenService.GenerateAccessTokenAsync(user);
        var (refreshEntity, rawToken) = await _tokenService.GenerateRefreshTokenAsync(user.Id, Ip(), UserAgent());

        return Ok(new AuthResponse(accessToken, rawToken, accessExp, refreshEntity.ExpiresAt, dto));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var norm = req.EmailOrUsername.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == norm || u.Username.ToLower() == norm);
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });

        if (!user.IsActive) return StatusCode(403, new { message = "Account deactivated" });

        if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
            return StatusCode(423, new { message = $"Account locked until {user.LockoutEnd:O}" });

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
            await _db.SaveChangesAsync();
            await AuditAsync("LOGIN_FAILED", user.Id, user.Id.ToString(), null, new { Attempt = user.FailedLoginAttempts, Ip = Ip() });
            return Unauthorized(new { message = "Invalid credentials" });
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await _db.SaveChangesAsync();

        var dto = await ToUserDtoAsync(user);
        var (accessToken, accessExp) = await _tokenService.GenerateAccessTokenAsync(user);
        var (refreshEntity, rawToken) = await _tokenService.GenerateRefreshTokenAsync(user.Id, Ip(), UserAgent());

        await AuditAsync("LOGIN_SUCCESS", user.Id, user.Id.ToString(), null, new { Ip = Ip() });

        return Ok(new AuthResponse(accessToken, rawToken, accessExp, refreshEntity.ExpiresAt, dto));
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest(new { message = "Refresh token required" });
        
        var hashed = _tokenService.HashToken(req.RefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hashed);
        if (stored == null) return Unauthorized(new { message = "Invalid refresh token" });
        
        if (stored.Revoked)
        {
            await _tokenService.RevokeAllUserTokensAsync(stored.UserId, Ip());
            return Unauthorized(new { message = "Token reuse detected" });
        }
        
        if (stored.IsExpired) return Unauthorized(new { message = "Refresh token expired" });

        var user = await _db.Users.FindAsync(stored.UserId);
        if (user == null || !user.IsActive) return Unauthorized(new { message = "User inactive" });

        var (newRefreshEntity, newRawToken) = await _tokenService.RotateRefreshTokenAsync(stored, Ip(), UserAgent());
        var (accessToken, accessExp) = await _tokenService.GenerateAccessTokenAsync(user);
        var dto = await ToUserDtoAsync(user);

        return Ok(new AuthResponse(accessToken, newRawToken, accessExp, newRefreshEntity.ExpiresAt, dto));
    }

    [Authorize]
    [HttpPost("revoke-token")]
    [HttpPost("logout")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest? req)
    {
        string? rawToken = req?.RefreshToken;
        if (string.IsNullOrWhiteSpace(rawToken) && Request.Headers.ContainsKey("X-Refresh-Token"))
            rawToken = Request.Headers["X-Refresh-Token"].ToString();

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (Guid.TryParse(uidStr, out var uid))
                {
                    await _tokenService.RevokeAllUserTokensAsync(uid, Ip());
                    return Ok(new { message = "All tokens revoked" });
                }
            }
            return BadRequest(new { message = "Refresh token required" });
        }

        var hashed = _tokenService.HashToken(rawToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hashed);
        if (stored != null)
        {
            stored.Revoked = true;
            stored.RevokedAt = DateTime.UtcNow;
            stored.RevokedByIp = Ip();
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Token revoked" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        var dto = await ToUserDtoAsync(user);
        return Ok(new MeResponse(dto));
    }
}