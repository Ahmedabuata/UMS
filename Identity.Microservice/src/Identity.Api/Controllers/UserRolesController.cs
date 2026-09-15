using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Identity.Api.Interfaces;
namespace Identity.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/roles")]
[Authorize]
public class UserRolesController : ControllerBase
{
    /// <summary>
    /// Database context instance used to access and manage application data.
    /// </summary>
    private readonly IdentityDbContext _db;

    /// <summary>
    /// Token service used for managing and revoking user session tokens securely.
    /// </summary>
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the UserRolesController class with database and token services.
    /// </summary>
    public UserRolesController(IdentityDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Records an audit log entry for security and tracking user role modifications.
    /// </summary>
    private async Task Audit(string action, Guid? uid, string? eid, object? oldV = null, object? newV = null)
    {
        var userIdClaim = User?.FindFirst("userId")?.Value;
        var createdBy = !string.IsNullOrEmpty(userIdClaim) ? Guid.Parse(userIdClaim) : (Guid?)null;

        _db.AuditLogs.Add(new AuditLog 
        { 
            Id = Guid.NewGuid(), 
            UserId = uid, 
            Action = action, 
            Entity = "user_roles", 
            EntityId = eid, 
            TableName = "user_roles",
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

    /// <summary>
    /// Retrieves all roles assigned to a specific user[cite: 13].
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RoleRead")]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId)) 
            return NotFound(new { message = "User not found" });

        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role != null)
            .Select(ur => new RoleDto(
                ur.Role!.Id,
                ur.Role.RoleName,
                ur.Role.DisplayName,
                ur.Role.Description,
                ur.Role.BranchCode,
                ur.Role.IsSystemRole,
                ur.Role.IsActive,
                ur.Role.CreatedAt,
                ur.Role.UpdatedAt,
                0
            ))
            .ToListAsync();

        return Ok(roles);
    }

    /// <summary>
    /// Assigns a new role to a specified user, updates user metadata, and generates an audit log entry[cite: 13].
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleRequest req)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId)) 
            return NotFound(new { message = "User not found" });
        if (!await _db.Roles.AnyAsync(r => r.Id == req.RoleId)) 
            return NotFound(new { message = "Role not found" });
        if (await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == req.RoleId)) 
            return Conflict(new { message = "Role already assigned" });

        var userRole = new UserRole 
        { 
            Id = Guid.NewGuid(), 
            UserId = userId, 
            RoleId = req.RoleId, 
            AssignedAt = DateTime.UtcNow 
        };

        _db.UserRoles.Add(userRole);

        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _db.Users.Update(user);
        }

        await _db.SaveChangesAsync();

        await Audit("ASSIGN_ROLE_TO_USER", userId, req.RoleId.ToString(), null, new { UserId = userId, RoleId = req.RoleId });

        return Ok(new { message = "Role assigned" });
    }

    /// <summary>
    /// Removes an assigned role from a specified user, enforces security validations for critical roles like SUPER_ADMIN, revokes active tokens securely, and records an audit log entry[cite: 13].
    /// </summary>
    [HttpDelete("{roleId}")]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId)
    {
        var ur = await _db.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);
        if (ur == null) return NotFound();

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role != null && role.RoleName == "SUPER_ADMIN")
        {
            var otherAdmin = await _db.UserRoles
                .AnyAsync(r => r.RoleId == roleId && r.UserId != userId);

            if (!otherAdmin)
            {
                return BadRequest(new { message = "Cannot remove the last SUPER_ADMIN role from the system." });
            }
        }

        // 1. Revoke active tokens if the removed role is SUPER_ADMIN
        if (role != null && role.RoleName == "SUPER_ADMIN")
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var currentUserId = User?.FindFirst("userId")?.Value;
            await _tokenService.RevokeAllUserTokensAsync(userId, remoteIp, currentUserId);
        }

        // 2. Remove the role assignment
        _db.UserRoles.Remove(ur);

        // 3. Update user metadata timestamp
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _db.Users.Update(user);
        }

        // 4. Save all pending changes to the database
        await _db.SaveChangesAsync();

        // 5. Record audit log entry
        await Audit("REMOVE_ROLE_FROM_USER", userId, roleId.ToString(), new { UserId = userId, RoleId = roleId }, null);

        return NoContent();
    }
}