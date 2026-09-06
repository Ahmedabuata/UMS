using System.Text.Json;
using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/roles/{roleId}/permissions")]
[Authorize]
public class RolePermissionsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    public RolePermissionsController(IdentityDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "RoleRead")]
    public async Task<IActionResult> GetRolePermissions(Guid roleId)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == roleId)) return NotFound(new { message = "Role not found" });
        
        var perms = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.Permission != null)
            .Select(rp => new PermissionDto(
                rp.Permission!.Id, 
                rp.Permission.PermissionName, 
                rp.Permission.Description, 
                rp.Permission.Module, 
                rp.Permission.ModuleCode, 
                rp.Permission.BranchCode, 
                rp.Permission.IsSensitive, 
                rp.Permission.IsActive, 
                rp.Permission.CreatedAt, 
                rp.Permission.UpdatedAt))
            .ToListAsync();
            
        return Ok(perms);
    }

    [HttpPost]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> AssignPermission(Guid roleId, [FromBody] AssignPermissionRequest req)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == roleId)) return NotFound(new { message = "Role not found" });
        if (!await _db.Permissions.AnyAsync(p => p.Id == req.PermissionId)) return NotFound(new { message = "Permission not found" });
        if (await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == req.PermissionId)) return Conflict(new { message = "Permission already assigned" });
        
        _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = req.PermissionId }); 
        await _db.SaveChangesAsync();
        return Ok(new { message = "Permission assigned" });
    }

    [HttpPut]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> BulkSync(Guid roleId, [FromBody] BulkSyncPermissionsRequest req)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == roleId)) return NotFound(new { message = "Role not found" });
        var existingPermIds = await _db.Permissions.Where(p => req.PermissionIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();
        if (existingPermIds.Count != req.PermissionIds.Distinct().Count()) return BadRequest(new { message = "One or more permissions not found" });
        
        var current = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(current);
        foreach (var pid in req.PermissionIds.Distinct()) _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid });
        await _db.SaveChangesAsync();
        
        _db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), Action = "BULK_SYNC_PERMISSIONS", EntityName = "role_permissions", EntityId = roleId.ToString(), NewValues = JsonSerializer.Serialize(new { PermissionIds = req.PermissionIds }), CreatedAt = DateTime.UtcNow }); 
        await _db.SaveChangesAsync();
        
        return Ok(new { message = "Permissions synced", count = req.PermissionIds.Count });
    }

    [HttpDelete("{permissionId}")]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId)
    {
        var rp = await _db.RolePermissions.FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);
        if (rp == null) return NotFound();
        _db.RolePermissions.Remove(rp); 
        await _db.SaveChangesAsync();
        return NoContent();
    }
}