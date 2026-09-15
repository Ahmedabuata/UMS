using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IdentityDbContext _db;

    public RolesController(IdentityDbContext db) => _db = db;

    // ============================================================
    // ✅ Protected Roles (cannot be deleted)
    // ============================================================
    private static readonly HashSet<string> ProtectedRoles = new(StringComparer.Ordinal)
    {
        "SUPER_ADMIN",
        "SUPER_USER"
    };

    // ============================================================
    // Helper: Check if user can manage roles
    // ============================================================
    private bool CanManageRoles()
    {
        return User.HasClaim("permission", "ROLE_WRITE")
            || User.HasClaim("permission", "ROLE_UPDATE")
            || User.HasClaim("permission", "ROLE_DELETE")
            || User.IsInRole("SUPER_ADMIN")
            || User.IsInRole("SUPER_USER")
            || User.IsInRole("ADMIN");
    }

    // ============================================================
    // Audit Logger
    // ============================================================
    private async Task Audit(string action, Guid? uid, string? eid, object? oldV = null, object? newV = null)
    {
        var userIdClaim = User?.FindFirst("userId")?.Value;
        var createdBy = !string.IsNullOrEmpty(userIdClaim) ? Guid.Parse(userIdClaim) : (Guid?)null;

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = uid,
            Action = action,
            Entity = "roles",
            EntityId = eid,
            TableName = "roles",
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
    // GET /api/roles
    // ============================================================
    [HttpGet]
    [Authorize(Policy = "RoleRead")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var query = CanManageRoles()
            ? _db.Roles.IgnoreQueryFilters().AsQueryable()
            : _db.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            
            query = query.Where(r =>
                EF.Functions.ILike(r.RoleName, pattern) ||
                (r.DisplayName != null && EF.Functions.ILike(r.DisplayName, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);

        var roles = await query
            .GroupJoin(
                _db.UserRoles,
                r => r.Id,
                ur => ur.RoleId,
                (r, urs) => new { Role = r, UserCount = urs.Count() })
            .OrderBy(x => x.Role.RoleName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new RoleDto(
                x.Role.Id,
                x.Role.RoleName,
                x.Role.DisplayName,
                x.Role.Description,
                x.Role.BranchCode,
                x.Role.IsSystemRole,
                x.Role.IsActive,
                x.Role.CreatedAt,
                x.Role.UpdatedAt,
                x.UserCount
            ))
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            data = roles,
            total,
            page,
            limit,
            totalPages = (int)Math.Ceiling((double)total / limit)
        });
    }

    // ============================================================
    // GET /api/roles/count
    // ============================================================
    [HttpGet("count")]
    [Authorize(Policy = "RoleRead")]
    public async Task<IActionResult> GetCount(CancellationToken cancellationToken = default)
    {
        var count = CanManageRoles()
            ? await _db.Roles.IgnoreQueryFilters().CountAsync(cancellationToken)
            : await _db.Roles.CountAsync(cancellationToken);

        return Ok(new { count });
    }

    // ============================================================
    // GET /api/roles/{id}
    // ============================================================
    [HttpGet("{id}")]
    [Authorize(Policy = "RoleRead")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var query = CanManageRoles()
            ? _db.Roles.IgnoreQueryFilters()
            : _db.Roles;

        var r = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (r == null) return NotFound(new { message = "Role not found" });

        var count = await _db.UserRoles.CountAsync(ur => ur.RoleId == id, cancellationToken);
        return Ok(new RoleDto(
            r.Id, r.RoleName, r.DisplayName, r.Description,
            r.BranchCode, r.IsSystemRole, r.IsActive,
            r.CreatedAt, r.UpdatedAt, count
        ));
    }

    // ============================================================
    // POST /api/roles
    // ============================================================
    [HttpPost]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest req, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(req.RoleName))
            return BadRequest(new { message = "RoleName required" });

        if (string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { message = "DisplayName required" });

        var name = req.RoleName.Trim().ToUpperInvariant();

        // ✅ Check duplicates including inactive roles
        if (await _db.Roles.IgnoreQueryFilters()
            .AnyAsync(r => r.RoleName == name, cancellationToken))
            return Conflict(new { message = "Role already exists (may be inactive)" });

        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = name,
            DisplayName = req.DisplayName.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            BranchCode = string.IsNullOrWhiteSpace(req.BranchCode) ? null : req.BranchCode.Trim(),
            IsSystemRole = req.IsSystemRole,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        await Audit("CREATE_ROLE", null, role.Id.ToString(), null,
            new { role.RoleName, role.DisplayName, role.IsSystemRole, role.IsActive });

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, new RoleDto(
            role.Id, role.RoleName, role.DisplayName, role.Description,
            role.BranchCode, role.IsSystemRole, role.IsActive,
            role.CreatedAt, role.UpdatedAt, 0
        ));
    }

    // ============================================================
    // PUT /api/roles/{id}
    // ============================================================
    [HttpPut("{id}")]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest req, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (role == null) return NotFound(new { message = "Role not found" });

        var old = new
        {
            role.RoleName, role.DisplayName, role.Description,
            role.BranchCode, role.IsSystemRole, role.IsActive
        };

        // ✅ RoleName
        if (!string.IsNullOrWhiteSpace(req.RoleName))
        {
            var newName = req.RoleName.Trim().ToUpperInvariant();
            if (newName != role.RoleName)
            {
                // ✅ Prevent renaming protected roles
                if (ProtectedRoles.Contains(role.RoleName))
                    return BadRequest(new { message = $"Cannot rename protected role {role.RoleName}" });

                if (await _db.Roles.IgnoreQueryFilters()
                    .AnyAsync(r => r.RoleName == newName && r.Id != id, cancellationToken))
                    return Conflict(new { message = "RoleName already exists" });
                role.RoleName = newName;
            }
        }

        // ✅ DisplayName
        if (!string.IsNullOrWhiteSpace(req.DisplayName))
            role.DisplayName = req.DisplayName.Trim();

        // ✅ Description
        if (req.Description != null)
            role.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();

        // ✅ BranchCode
        if (req.BranchCode != null)
            role.BranchCode = string.IsNullOrWhiteSpace(req.BranchCode) ? null : req.BranchCode.Trim();

        // ✅ IsSystemRole
        if (req.IsSystemRole.HasValue)
        {
            if (ProtectedRoles.Contains(role.RoleName) && req.IsSystemRole.Value == false)
                return BadRequest(new { message = $"Cannot disable system role for {role.RoleName}" });
            role.IsSystemRole = req.IsSystemRole.Value;
        }

        // ✅ IsActive
        if (req.IsActive.HasValue)
        {
            if (ProtectedRoles.Contains(role.RoleName) && req.IsActive.Value == false)
                return BadRequest(new { message = $"Cannot deactivate {role.RoleName} role" });
            role.IsActive = req.IsActive.Value;
        }

        role.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await Audit("UPDATE_ROLE", null, id.ToString(), old,
            new { role.RoleName, role.DisplayName, role.Description, role.BranchCode, role.IsSystemRole, role.IsActive });

        var count = await _db.UserRoles.CountAsync(ur => ur.RoleId == id, cancellationToken);
        return Ok(new RoleDto(
            role.Id, role.RoleName, role.DisplayName, role.Description,
            role.BranchCode, role.IsSystemRole, role.IsActive,
            role.CreatedAt, role.UpdatedAt, count
        ));
    }

    // ============================================================
    // PATCH /api/roles/{id}/status
    // ============================================================
    [HttpPatch("{id}/status")]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> PatchStatus(
        Guid id,
        [FromBody] PatchRoleStatusRequest req,
        CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (role == null) return NotFound(new { message = "Role not found" });

        // ✅ Prevent deactivating protected roles
        if (ProtectedRoles.Contains(role.RoleName) && !req.IsActive)
            return BadRequest(new { message = $"Cannot deactivate {role.RoleName} role" });

        var old = role.IsActive;
        role.IsActive = req.IsActive;
        role.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await Audit(
            req.IsActive ? "ACTIVATE_ROLE" : "DEACTIVATE_ROLE",
            null,
            id.ToString(),
            new { IsActive = old },
            new { IsActive = req.IsActive });

        return Ok(new
        {
            id = role.Id,
            roleName = role.RoleName,
            displayName = role.DisplayName,
            isActive = role.IsActive,
            updatedAt = role.UpdatedAt
        });
    }

    // ============================================================
    // DELETE /api/roles/{id}
    // ✅ Protected: SUPER_ADMIN, SUPER_USER
    // ✅ Check 1: Protected roles
    // ✅ Check 2: Assigned users
    // ✅ Check 3: Assigned permissions (RolePermissions)
    // ============================================================
    [HttpDelete("{id}")]
    [Authorize(Policy = "RoleDelete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (role == null) return NotFound(new { message = "Role not found" });

        // ✅ Check 1: Protected roles
        if (ProtectedRoles.Contains(role.RoleName))
            return BadRequest(new
            {
                code = "PROTECTED_ROLE",
                message = $"Cannot delete {role.RoleName} role. It is a protected system role."
            });

        // ✅ Check 2: Assigned users
        var userCount = await _db.UserRoles
            .CountAsync(ur => ur.RoleId == id, cancellationToken);
        if (userCount > 0)
            return BadRequest(new
            {
                code = "ROLE_IN_USE",
                message = $"Cannot delete role with {userCount} assigned user(s)"
            });

        // ✅ Check 3: Assigned permissions (explicit)
        var permissionCount = await _db.RolePermissions
            .CountAsync(rp => rp.RoleId == id, cancellationToken);
        if (permissionCount > 0)
            return BadRequest(new
            {
                code = "ROLE_HAS_PERMISSIONS",
                message = $"Cannot delete role with {permissionCount} assigned permission(s). Remove them first."
            });

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync(cancellationToken);

        await Audit("DELETE_ROLE", null, id.ToString(), new { role.RoleName }, null);

        return NoContent();
    }
}