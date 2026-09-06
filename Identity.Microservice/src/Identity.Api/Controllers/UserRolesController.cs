using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/roles")]
[Authorize]
public class UserRolesController : ControllerBase
{
    private readonly IdentityDbContext _db;
    public UserRolesController(IdentityDbContext db) => _db = db;

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

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = req.RoleId });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Role assigned" });
    }

    [HttpDelete("{roleId}")]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId)
    {
        var ur = await _db.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);
        if (ur == null) return NotFound();

        _db.UserRoles.Remove(ur);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
