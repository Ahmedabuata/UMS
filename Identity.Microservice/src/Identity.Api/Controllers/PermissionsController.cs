using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Identity.Api.Interfaces;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<PermissionsController> _logger;

    // ============================================================
    // ✅ Protected Permissions (cannot be deleted)
    // ============================================================
    private static readonly HashSet<string> ProtectedPermissions = new(StringComparer.Ordinal)
    {
        "SUPER_ADMIN",
        "SUPER_USER"
    };

    public PermissionsController(
        IdentityDbContext db,
        ITokenService tokenService,
        ILogger<PermissionsController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
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
            Entity = "permissions",
            EntityId = eid,
            TableName = "permissions",
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
    // Helper: Revoke tokens for all users with this permission
    // ============================================================
    private async Task RevokeTokensForPermissionUsersAsync(Guid permissionId)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var currentUserId = User?.FindFirst("userId")?.Value;

        var userIds = await _db.RolePermissions
            .Where(rp => rp.PermissionId == permissionId)
            .Join(_db.UserRoles, rp => rp.RoleId, ur => ur.RoleId, (rp, ur) => ur.UserId)
            .Distinct()
            .ToListAsync();

        if (!userIds.Any()) return;

        foreach (var userId in userIds)
        {
            await _tokenService.RevokeAllUserTokensAsync(userId, remoteIp, currentUserId);
        }
    }

    // ============================================================
    // GET /api/permissions
    // ============================================================
    [HttpGet]
    [Authorize(Policy = "PermissionRead")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.Permissions
                .IgnoreQueryFilters()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.PermissionName.ToLower().Contains(s) ||
                    (p.Description != null && p.Description.ToLower().Contains(s)) ||
                    (p.Module != null && p.Module.ToLower().Contains(s))
                );
            }

            query = query.OrderBy(p => p.PermissionName);
            var total = await query.CountAsync(cancellationToken);

            var permissions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PermissionDto(
                    p.Id,
                    p.PermissionName,
                    p.Description,
                    p.Module,
                    p.ModuleCode,
                    p.BranchCode,
                    p.IsSensitive,
                    p.IsActive,
                    p.CreatedAt,
                    p.UpdatedAt))
                .ToListAsync(cancellationToken);

            return Ok(new { items = permissions, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions list");
            return StatusCode(500, new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "An unexpected error occurred",
                error = ex.Message
            });
        }
    }

    // ============================================================
    // GET /api/permissions/{id}
    // ============================================================
    [HttpGet("{id}")]
    [Authorize(Policy = "PermissionRead")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var p = await _db.Permissions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (p == null)
                return NotFound(new { code = "PERMISSION_NOT_FOUND", message = "Permission not found" });

            return Ok(new PermissionDto(
                p.Id,
                p.PermissionName,
                p.Description,
                p.Module,
                p.ModuleCode,
                p.BranchCode,
                p.IsSensitive,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission {PermissionId}", id);
            return StatusCode(500, new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "An unexpected error occurred",
                error = ex.Message
            });
        }
    }

    // ============================================================
    // POST /api/permissions
    // ============================================================
    [HttpPost]
    [Authorize(Policy = "PermissionWrite")]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.PermissionName))
                return BadRequest(new { code = "PERMISSION_NAME_REQUIRED", message = "PermissionName is required" });

            if (string.IsNullOrWhiteSpace(req.Module))
                return BadRequest(new { code = "MODULE_REQUIRED", message = "Module is required" });

            var name = req.PermissionName.Trim().ToUpperInvariant();

            if (await _db.Permissions.IgnoreQueryFilters()
                .AnyAsync(x => x.PermissionName == name, cancellationToken))
                return Conflict(new { code = "DUPLICATE_PERMISSION", message = "Permission already exists" });

            var perm = new Permission
            {
                Id = Guid.NewGuid(),
                PermissionName = name,
                Description = req.Description?.Trim() ?? name,
                Module = req.Module.Trim(),
                ModuleCode = req.ModuleCode?.Trim(),
                BranchCode = string.IsNullOrWhiteSpace(req.BranchCode) ? null : req.BranchCode.Trim(),
                IsSensitive = req.IsSensitive,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Permissions.Add(perm);
            await _db.SaveChangesAsync(cancellationToken);

            await Audit("CREATE_PERMISSION", null, perm.Id.ToString(), null,
                new { perm.PermissionName, perm.Module });

            _logger.LogInformation("Permission created: {PermissionName}", perm.PermissionName);

            return CreatedAtAction(nameof(GetById), new { id = perm.Id }, new PermissionDto(
                perm.Id,
                perm.PermissionName,
                perm.Description,
                perm.Module,
                perm.ModuleCode,
                perm.BranchCode,
                perm.IsSensitive,
                perm.IsActive,
                perm.CreatedAt,
                perm.UpdatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission {PermissionName}", req?.PermissionName);
            return StatusCode(500, new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "An unexpected error occurred",
                error = ex.Message
            });
        }
    }

    // ============================================================
    // PUT /api/permissions/{id}
    // ============================================================
    [HttpPut("{id}")]
    [Authorize(Policy = "PermissionWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var perm = await _db.Permissions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (perm == null)
                return NotFound(new { code = "PERMISSION_NOT_FOUND", message = "Permission not found" });

            var oldValues = new { perm.Description, perm.Module, perm.IsSensitive, perm.IsActive };

            if (req.Description != null) perm.Description = req.Description.Trim();
            if (req.Module != null) perm.Module = req.Module.Trim();
            if (req.ModuleCode != null) perm.ModuleCode = req.ModuleCode.Trim();
            if (req.BranchCode != null) perm.BranchCode = string.IsNullOrWhiteSpace(req.BranchCode) ? null : req.BranchCode.Trim();
            if (req.IsSensitive.HasValue) perm.IsSensitive = req.IsSensitive.Value;
            if (req.IsActive.HasValue) perm.IsActive = req.IsActive.Value;

            perm.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await RevokeTokensForPermissionUsersAsync(id);
            await Audit("UPDATE_PERMISSION", null, id.ToString(), oldValues,
                new { perm.Description, perm.Module, perm.IsSensitive, perm.IsActive });

            _logger.LogInformation("Permission updated: {PermissionId}", id);

            return Ok(new PermissionDto(
                perm.Id,
                perm.PermissionName,
                perm.Description,
                perm.Module,
                perm.ModuleCode,
                perm.BranchCode,
                perm.IsSensitive,
                perm.IsActive,
                perm.CreatedAt,
                perm.UpdatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionId}", id);
            return StatusCode(500, new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "An unexpected error occurred",
                error = ex.Message
            });
        }
    }

    // ============================================================
    // PATCH /api/permissions/{id}/status
    // ============================================================
    [HttpPatch("{id}/status")]
    [Authorize(Policy = "PermissionWrite")]
    public async Task<IActionResult> PatchStatus(
        Guid id,
        [FromBody] PatchPermissionStatusRequest req,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var perm = await _db.Permissions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (perm == null)
                return NotFound(new { code = "PERMISSION_NOT_FOUND", message = "Permission not found" });

            // ✅ Prevent deactivating protected permissions
            if (ProtectedPermissions.Contains(perm.PermissionName) && !req.IsActive)
                return BadRequest(new
                {
                    code = "PROTECTED_PERMISSION",
                    message = $"Cannot deactivate {perm.PermissionName} permission"
                });

            var old = perm.IsActive;
            perm.IsActive = req.IsActive;
            perm.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            if (!req.IsActive)
            {
                await RevokeTokensForPermissionUsersAsync(id);
            }

            await Audit(
                req.IsActive ? "ACTIVATE_PERMISSION" : "DEACTIVATE_PERMISSION",
                null,
                id.ToString(),
                new { IsActive = old },
                new { IsActive = req.IsActive });

            _logger.LogInformation("Permission {PermissionId} status changed to {IsActive}", id, req.IsActive);

            return Ok(new
            {
                id = perm.Id,
                permission_name = perm.PermissionName,
                is_active = perm.IsActive,
                updated_at = perm.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling permission status {PermissionId}", id);
            return StatusCode(500, new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "An unexpected error occurred",
                error = ex.Message
            });
        }
    }

    // ============================================================
    // PATCH /api/permissions/{id}/sensitivity
    // ============================================================
    [HttpPatch("{id}/sensitivity")]
    [Authorize(Policy = "PermissionWrite")]
    public async Task<IActionResult> PatchSensitivity(
        Guid id,
        [FromBody] PatchPermissionSensitivityRequest req,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var perm = await _db.Permissions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (perm == null)
                return NotFound(new { code = "PERMISSION_NOT_FOUND", message = "Permission not found" });

            // ✅ Prevent unmarking protected permissions from being sensitive? (optional)
            // For now, allow toggling sensitivity on protected permissions.

            var old = perm.IsSensitive;
            perm.IsSensitive = req.IsSensitive;
            perm.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            await Audit(
                req.IsSensitive ? "MARK_PERMISSION_SENSITIVE" : "UNMARK_PERMISSION_SENSITIVE",
                null,
                id.ToString(),
                new { IsSensitive = old },
                new { IsSensitive = req.IsSensitive });

            _logger.LogInformation(
                "Permission {PermissionId} sensitivity changed to {IsSensitive}",
                id, req.IsSensitive);

            return Ok(new
            {
                id = perm.Id,
                permission_name = perm.PermissionName,
                is_sensitive = perm.IsSensitive,
                updated_at = perm.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling permission sensitivity {PermissionId}", id);
            return StatusCode(500, new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "An unexpected error occurred",
                error = ex.Message
            });
        }
    }

    // ============================================================
    // DELETE /api/permissions/{id}
    // ✅ Protected: SUPER_ADMIN, SUPER_USER
    // ✅ Check 1: Protected permissions
    // ✅ Check 2: In use by roles
    // ✅ Check 3: Sensitive
    // ============================================================
    [HttpDelete("{id}")]
    [Authorize(Policy = "PermissionDelete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var perm = await _db.Permissions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (perm == null)
                return NotFound(new { code = "PERMISSION_NOT_FOUND", message = "Permission not found" });

            // ✅ Check 1: Protected permissions
            if (ProtectedPermissions.Contains(perm.PermissionName))
                return BadRequest(new
                {
                    code = "PROTECTED_PERMISSION",
                    message = $"Cannot delete {perm.PermissionName} permission. It is a protected system permission."
                });

            // ✅ Check 2: In use by roles
            var roleCount = await _db.RolePermissions
                .CountAsync(rp => rp.PermissionId == id, cancellationToken);

            if (roleCount > 0)
                return BadRequest(new
                {
                    code = "PERMISSION_IN_USE",
                    message = $"Cannot delete permission. It is assigned to {roleCount} role(s)."
                });

            // ✅ Check 3: Sensitive
            if (perm.IsSensitive)
                return BadRequest(new
                {
                    code = "SENSITIVE_PERMISSION",
                    message = "Cannot delete sensitive permission. Unmark it as sensitive first."
                });

            await RevokeTokensForPermissionUsersAsync(id);

            _db.Permissions.Remove(perm);
            await _db.SaveChangesAsync(cancellationToken);

            await Audit("DELETE_PERMISSION", null, id.ToString(), new { perm.PermissionName }, null);
            _logger.LogInformation("Permission deleted: {PermissionName}", perm.PermissionName);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission {PermissionId}", id);
            return StatusCode(500, new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "An unexpected error occurred",
                error = ex.Message
            });
        }
    }
}