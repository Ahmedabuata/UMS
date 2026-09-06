using System.Text.Json;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
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
    public UsersController(IdentityDbContext db) => _db = db;

    private async Task Audit(string action, Guid? uid, string? eid, object? oldV = null, object? newV = null)
    {
        _db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), UserId = uid, Action = action, EntityName = "users", EntityId = eid, OldValues = oldV != null ? JsonSerializer.Serialize(oldV) : null, NewValues = newV != null ? JsonSerializer.Serialize(newV) : null, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
    }

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

        return new UserDto(u.Id, u.Username, u.Email, u.PhoneNumber, u.IsActive, u.MustChangePassword, u.CreatedAt, roles, perms, groups);
    }

    [HttpGet]
    [Authorize(Policy = "UserRead")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToLower(); q = q.Where(u => u.Username.ToLower().Contains(s) || u.Email.ToLower().Contains(s)); }
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var dtos = new List<UserDto>(); foreach (var u in items) dtos.Add(await ToDto(u));
        return Ok(new UserListResponse(dtos, total, page, pageSize));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "UserRead")]
    public async Task<IActionResult> GetById(Guid id) { var u = await _db.Users.FindAsync(id); if (u == null) return NotFound(); return Ok(await ToDto(u)); }

    [HttpPost]
    [Authorize(Policy = "UserWrite")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var err = GlobalValidators.ValidateUserInput(req.Email, req.Username, req.PhoneNumber); if (err != null) return BadRequest(new { message = err });
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8) return BadRequest(new { message = "Password >=8" });
        var emailNorm = GlobalValidators.NormalizeEmail(req.Email); var userNorm = GlobalValidators.NormalizeUsername(req.Username);
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm.ToLower())) return Conflict(new { message = "Email exists" });
        if (await _db.Users.AnyAsync(u => u.Username == userNorm)) return Conflict(new { message = "Username exists" });
        
        var user = new User { Id = Guid.NewGuid(), Username = userNorm, Email = emailNorm, PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password), PhoneNumber = req.PhoneNumber?.Trim(), IsActive = true, MustChangePassword = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.Add(user);

        string roleToAssign = string.IsNullOrWhiteSpace(req.RoleName) ? "USER" : req.RoleName.Trim().ToUpperInvariant();
        if (!GlobalValidators.AllowedRoles.Contains(roleToAssign)) return BadRequest(new { message = $"Invalid RoleName. Allowed: {string.Join(",", GlobalValidators.AllowedRoles)}" });
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleToAssign);
        if (role != null) _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        await _db.SaveChangesAsync(); await Audit("CREATE_USER", null, user.Id.ToString(), null, new { user.Email, user.Username, role = roleToAssign });
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, await ToDto(user));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "UserWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req)
    {
        var u = await _db.Users.FindAsync(id); if (u == null) return NotFound(); 
        var old = new { u.Email, u.Username, u.PhoneNumber };
        if (req.Email != null) { if (!GlobalValidators.IsValidEmail(req.Email)) return BadRequest(new { message = "Invalid email" }); var norm = GlobalValidators.NormalizeEmail(req.Email); if (await _db.Users.AnyAsync(x => x.Id != id && x.Email.ToLower() == norm.ToLower())) return Conflict(new { message = "Email exists" }); u.Email = norm; }
        if (req.Username != null) { if (!GlobalValidators.IsValidUsername(req.Username)) return BadRequest(new { message = "Invalid username" }); var norm = GlobalValidators.NormalizeUsername(req.Username); if (await _db.Users.AnyAsync(x => x.Id != id && x.Username == norm)) return Conflict(new { message = "Username exists" }); u.Username = norm; }
        if (req.PhoneNumber != null) { if (!GlobalValidators.IsValidPhone(req.PhoneNumber)) return BadRequest(new { message = "Invalid phone" }); u.PhoneNumber = req.PhoneNumber.Trim(); }
        u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(); await Audit("UPDATE_USER", null, id.ToString(), old, new { u.Email, u.Username, u.PhoneNumber });
        return Ok(await ToDto(u));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Policy = "UserWrite")]
    public async Task<IActionResult> PatchStatus(Guid id, [FromBody] PatchUserStatusRequest req)
    { 
        var u = await _db.Users.FindAsync(id); if (u == null) return NotFound(); 
        var old = u.IsActive; u.IsActive = req.IsActive; u.UpdatedAt = DateTime.UtcNow; 
        await _db.SaveChangesAsync(); 
        await Audit(req.IsActive ? "ACTIVATE_USER" : "DEACTIVATE_USER", null, id.ToString(), new { IsActive = old }, new { IsActive = req.IsActive }); 
        
        if (!req.IsActive) { 
            var tokens = await _db.RefreshTokens.Where(t => t.UserId == id && !t.Revoked).ToListAsync(); 
            foreach (var t in tokens) { t.Revoked = true; t.RevokedAt = DateTime.UtcNow; } 
            await _db.SaveChangesAsync(); 
        } 
        return Ok(await ToDto(u)); 
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "UserDelete")]
    public async Task<IActionResult> Delete(Guid id)
    { 
        var u = await _db.Users.FindAsync(id); if (u == null) return NotFound(); 
        u.IsActive = false; u.UpdatedAt = DateTime.UtcNow; 
        await _db.SaveChangesAsync(); 
        var tokens = await _db.RefreshTokens.Where(t => t.UserId == id && !t.Revoked).ToListAsync(); 
        foreach (var t in tokens) { t.Revoked = true; t.RevokedAt = DateTime.UtcNow; } 
        await _db.SaveChangesAsync(); 
        await Audit("SOFT_DELETE_USER", null, id.ToString(), new { IsActive = true }, new { IsActive = false }); 
        return NoContent(); 
    }
}