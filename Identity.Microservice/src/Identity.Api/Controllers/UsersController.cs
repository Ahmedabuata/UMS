using System.Security.Claims;
using System.Text.Json;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Interfaces;
using Identity.Api.Models;
using Identity.Api.Services;
using Identity.Api.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[EnableRateLimiting("api")]
public class UsersController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IPasswordGenerator _passwordGenerator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IdentityDbContext db,
        ITokenService tokenService,
        IEmailService emailService,
        IPasswordGenerator passwordGenerator,
        ILogger<UsersController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
        _passwordGenerator = passwordGenerator;
        _logger = logger;
    }

    // ============================================================
    // Helper: Extract userId from JWT (Fallback Chain)
    // ============================================================
    /// <summary>
    /// Extracts the current user's ID from JWT claims using a fallback chain
    /// to support multiple JWT formats (NameIdentifier, sub, userId, uid).
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value
                      ?? User?.FindFirst("userId")?.Value
                      ?? User?.FindFirst("uid")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            return null;

        return Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : null;
    }

    // ============================================================
    // Helper: Audit Logger (Improved with Fallback Chain)
    // ============================================================
    private async Task Audit(string action, Guid? uid, string? eid, object? oldV = null, object? newV = null)
    {
        var createdBy = GetCurrentUserId();

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = uid,
            Action = action,
            Entity = "users",
            EntityId = eid,
            TableName = "users",
            OldValues = oldV != null ? JsonSerializer.Serialize(oldV) : null,
            NewValues = newV != null ? JsonSerializer.Serialize(newV) : null,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            CreatedBy = createdBy,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // ============================================================
    // Helper: User → UserDto
    // ============================================================
    private async Task<UserDto> ToDto(User u)
    {
        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == u.Id)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role != null ? ur.Role.RoleName : string.Empty)
            .Where(r => !string.IsNullOrEmpty(r))
            .ToListAsync();

        var perms = await _db.UserRoles
            .Where(ur => ur.UserId == u.Id)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p != null ? p.PermissionName : string.Empty)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToListAsync();

        var groups = await _db.UserGroups
            .Where(ug => ug.UserId == u.Id)
            .Include(ug => ug.Group)
            .Select(ug => ug.Group != null ? ug.Group.Name : string.Empty)
            .Where(g => !string.IsNullOrEmpty(g))
            .ToListAsync();

        var profileEntity = await _db.UserProfiles
            .FirstOrDefaultAsync(up => up.UserId == u.Id);

        UserProfileDto? profileDto = profileEntity != null ? new UserProfileDto(
            profileEntity.Id,
            profileEntity.UserId,
            profileEntity.Address,
            profileEntity.City,
            profileEntity.PostalCode,
            profileEntity.MaritalStatus,
            profileEntity.DateOfBirth,
            profileEntity.Gender,
            profileEntity.CreatedAt,
            profileEntity.UpdatedAt
        ) : null;

        return new UserDto(
            u.Id,
            u.Username,
            u.Email,
            u.PhoneNumber,
            u.IsActive,
            u.MustChangePassword,
            u.FailedLoginAttempts,
            u.LockoutEnd,
            u.FirstName,
            u.LastName,
            u.CreatedAt,
            u.UpdatedAt,
            roles,
            perms,
            groups,
            profileDto
        );
    }

    // ============================================================
    // GET: /api/users
    // ============================================================
    [HttpGet]
    [Authorize(Policy = "UserRead")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(u =>
                u.Username.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(s)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(s))
            );
        }
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var dtos = new List<UserDto>(); foreach (var u in items) dtos.Add(await ToDto(u));
        return Ok(new UserListResponse(dtos, total, page, pageSize));
    }

    // ============================================================
    // GET: /api/users/count
    // ============================================================
    [HttpGet("count")]
    [Authorize(Policy = "UserRead")]
    public async Task<IActionResult> GetCount()
    {
        var count = await _db.Users.CountAsync();
        return Ok(new { count });
    }

    // ============================================================
    // GET: /api/users/{id}
    // ============================================================
    [HttpGet("{id}")]
    [Authorize(Policy = "UserRead")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        return Ok(await ToDto(u));
    }

    // ============================================================
    // GET: /api/users/me  (Current User Profile)
    // ============================================================
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { code = "INVALID_TOKEN", message = "Invalid user token." });

        var u = await _db.Users.FindAsync(userId.Value);
        if (u == null) return NotFound(new { code = "USER_NOT_FOUND", message = "User not found." });

        return Ok(await ToDto(u));
    }

    // ============================================================
    // POST: /api/users
    // ============================================================
    /// <summary>
    /// Creates a new user.
    /// 
    /// Password handling:
    /// - If SendPasswordByEmail = true  → Backend generates random password + sends it via email.
    ///                                     MustChangePassword is set to TRUE.
    /// - If SendPasswordByEmail = false → Password is required from the request.
    ///                                     MustChangePassword is set to FALSE.
    /// 
    /// NOTE: Validation is handled by FluentValidation (ValidationFilter).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "UserWrite")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequestDto req)
    {
        // ============================================================
        // Normalize inputs
        // ============================================================
        var emailNorm = GlobalValidators.NormalizeEmail(req.Email);
        var userNorm = GlobalValidators.NormalizeUsername(req.Username);

        // ============================================================
        // Uniqueness checks
        // ============================================================
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm.ToLower()))
            return Conflict(new { message = "Email exists" });

        if (await _db.Users.AnyAsync(u => u.Username == userNorm))
            return Conflict(new { message = "Username exists" });

        // ============================================================
        // Determine password + MustChangePassword flag
        // ============================================================
        string plainPassword;
        bool sendEmail = req.SendPasswordByEmail;

        if (sendEmail || string.IsNullOrWhiteSpace(req.Password))
        {
            // Generate random password (12 chars)
            plainPassword = _passwordGenerator.Generate(12);
            sendEmail = true;
        }
        else
        {
            // Use provided password
            plainPassword = req.Password!;
        }

        // ============================================================
        // Create user entity
        // ============================================================
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = userNorm,
            Email = emailNorm,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
            PhoneNumber = req.PhoneNumber?.Trim(),
            FirstName = req.FirstName?.Trim(),
            LastName = req.LastName?.Trim(),
            IsActive = true,
            MustChangePassword = sendEmail,   // TRUE when password sent by email
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);

        // ============================================================
        // Assign role
        // ============================================================
        string roleToAssign = string.IsNullOrWhiteSpace(req.RoleName)
            ? "USER"
            : req.RoleName.Trim().ToUpperInvariant();

        if (!GlobalValidators.AllowedRoles.Contains(roleToAssign))
            return BadRequest(new { message = $"Invalid RoleName. Allowed: {string.Join(",", GlobalValidators.AllowedRoles)}" });

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleToAssign);
        if (role != null)
            _db.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow
            });

        await _db.SaveChangesAsync();

        // ============================================================
        // Send welcome email (if applicable)
        // ============================================================
        bool emailSent = false;
        if (sendEmail && !string.IsNullOrEmpty(user.Email))
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.Username, plainPassword);
                emailSent = true;
                _logger.LogInformation("Welcome email sent successfully to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                // Don't fail the whole request if email fails
                // User is created; admin can resend later
                _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
            }
        }

        // ============================================================
        // Audit log
        // ============================================================
        await Audit("CREATE_USER", user.Id, user.Id.ToString(), null,
            new { user.Email, user.Username, role = roleToAssign, emailSent });

        // ============================================================
        // Return 201 Created with emailSent flag
        // ============================================================
        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new
            {
                user = await ToDto(user),
                emailSent = emailSent
            });
    }

    // ============================================================
    // PUT: /api/users/{id}  (Admin only)
    // ============================================================
    [HttpPut("{id}")]
    [Authorize(Policy = "UserWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        var old = new { u.Email, u.Username, u.PhoneNumber, u.FirstName, u.LastName };

        if (req.Email != null)
        {
            if (!GlobalValidators.IsValidEmail(req.Email)) return BadRequest(new { message = "Invalid email" });
            var norm = GlobalValidators.NormalizeEmail(req.Email);
            if (await _db.Users.AnyAsync(x => x.Id != id && x.Email.ToLower() == norm.ToLower())) return Conflict(new { message = "Email exists" });
            u.Email = norm;
        }
        if (req.Username != null)
        {
            if (!GlobalValidators.IsValidUsername(req.Username)) return BadRequest(new { message = "Invalid username" });
            var norm = GlobalValidators.NormalizeUsername(req.Username);
            if (await _db.Users.AnyAsync(x => x.Id != id && x.Username == norm)) return Conflict(new { message = "Username exists" });
            u.Username = norm;
        }
        if (req.PhoneNumber != null)
        {
            if (!GlobalValidators.IsValidPhone(req.PhoneNumber)) return BadRequest(new { message = "Invalid phone" });
            u.PhoneNumber = req.PhoneNumber.Trim();
        }
        if (req.FirstName != null) { u.FirstName = req.FirstName.Trim(); }
        if (req.LastName != null) { u.LastName = req.LastName.Trim(); }
        if (req.IsActive.HasValue) { u.IsActive = req.IsActive.Value; }

        u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await Audit("UPDATE_USER", id, id.ToString(), old, new { u.Email, u.Username, u.PhoneNumber, u.FirstName, u.LastName });

        return Ok(await ToDto(u));
    }

    // ============================================================
    // PUT: /api/users/me  (Self-Service - IDOR-Proof)
    // ============================================================
    /// <summary>
    /// Updates the CURRENT user's profile (self-service).
    /// 
    /// Security:
    /// - Requires only authentication (no UserWrite policy).
    /// - userId is extracted from JWT (not URL).
    /// - IDOR-Proof: cannot modify other users.
    /// - Cannot change: username, is_active, roles.
    /// - Can change: email, phone, firstName, lastName.
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest req)
    {
        try
        {
            // 1. Extract userId from JWT
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { code = "INVALID_TOKEN", message = "Invalid user token." });

            // 2. Load user
            var u = await _db.Users.FindAsync(userId.Value);
            if (u == null)
                return NotFound(new { code = "USER_NOT_FOUND", message = "User not found." });

            var old = new { u.Email, u.PhoneNumber, u.FirstName, u.LastName };

            // 3. Update email (with uniqueness check)
            if (!string.IsNullOrWhiteSpace(req.Email))
            {
                if (!GlobalValidators.IsValidEmail(req.Email))
                    return BadRequest(new { code = "INVALID_EMAIL", message = "Invalid email." });

                var norm = GlobalValidators.NormalizeEmail(req.Email);
                if (await _db.Users.AnyAsync(x => x.Id != userId.Value && x.Email.ToLower() == norm.ToLower()))
                    return Conflict(new { code = "EMAIL_EXISTS", message = "Email already in use." });

                u.Email = norm;
            }

            // 4. Update phone (optional)
            if (req.PhoneNumber != null)
            {
                if (!GlobalValidators.IsValidPhone(req.PhoneNumber))
                    return BadRequest(new { code = "INVALID_PHONE", message = "Invalid phone number." });
                u.PhoneNumber = req.PhoneNumber.Trim();
            }

            // 5. Update name (optional)
            if (req.FirstName != null) u.FirstName = req.FirstName.Trim();
            if (req.LastName != null) u.LastName = req.LastName.Trim();

            // ⚠️ SECURITY: Do NOT update:
            // - Username (immutable)
            // - IsActive (admin only)
            // - Roles (admin only)

            u.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // 6. Audit
            await Audit("UPDATE_OWN_PROFILE", userId.Value, userId.Value.ToString(), old,
                new { u.Email, u.PhoneNumber, u.FirstName, u.LastName });

            return Ok(await ToDto(u));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating own profile for user");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred." });
        }
    }

    // ============================================================
    // PATCH: /api/users/{id}/status
    // ============================================================
    [HttpPatch("{id}/status")]
    [Authorize(Policy = "UserWrite")]
    public async Task<IActionResult> PatchStatus(Guid id, [FromBody] PatchUserStatusRequest req)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        var old = u.IsActive;
        u.IsActive = req.IsActive;
        u.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update user status due to a database error.", error = ex.Message });
        }

        try
        {
            await Audit(req.IsActive ? "ACTIVATE_USER" : "DEACTIVATE_USER", id, id.ToString(), new { IsActive = old }, new { IsActive = req.IsActive });
        }
        catch
        {
            // Suppress secondary audit log exceptions
        }

        if (!req.IsActive)
        {
            try
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userId = GetCurrentUserId()?.ToString();
                await _tokenService.RevokeAllUserTokensAsync(id, remoteIp, userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke tokens for user {UserId}", id);
            }
        }

        var response = new
        {
            id = u.Id,
            username = u.Username,
            email = u.Email,
            phoneNumber = u.PhoneNumber,
            isActive = u.IsActive,
            mustChangePassword = u.MustChangePassword,
            updatedAt = u.UpdatedAt
        };

        return Ok(response);
    }

    // ============================================================
    // DELETE: /api/users/{id}
    // ============================================================
    [HttpDelete("{id}")]
    [Authorize(Policy = "UserDelete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        var isSuperAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == id && ur.Role != null && ur.Role.RoleName == "SUPER_ADMIN");
        if (isSuperAdmin)
            return BadRequest(new { message = "Cannot delete user with SUPER_ADMIN role" });

        u.IsActive = false;
        u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userId = GetCurrentUserId()?.ToString();
        await _tokenService.RevokeAllUserTokensAsync(id, remoteIp, userId);

        await Audit("SOFT_DELETE_USER", id, id.ToString(), new { IsActive = true }, new { IsActive = false });

        return NoContent();
    }
}