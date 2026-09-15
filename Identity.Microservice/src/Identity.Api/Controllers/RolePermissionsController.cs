using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using Identity.Api.Interfaces;

namespace Identity.Api.Controllers;

/// <summary>
/// Handles role-permission management operations.
/// 
/// This controller:
/// - Lists permissions assigned to a role.
/// - Assigns a single permission (POST).
/// - Syncs permissions in bulk (PUT) — GOLDEN RULE #11.
/// - Removes a single permission (DELETE).
/// 
/// ALL operations:
/// - Are audited in audit_logs table (with old/new values).
/// - Revoke tokens for role users (force re-login).
/// - Are wrapped in transactions where needed.
/// </summary>
[ApiController]
[Route("api/roles/{roleId}/permissions")]
[Authorize]
public class RolePermissionsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RolePermissionsController> _logger;

    public RolePermissionsController(
        IdentityDbContext db,
        ITokenService tokenService,
        ILogger<RolePermissionsController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    // ============================================================
    // Helper: Extract userId from JWT with Fallback Chain
    // ============================================================
    /// <summary>
    /// Extracts the userId from JWT claims using a fallback chain
    /// to support multiple JWT formats (NameIdentifier, sub, userId, uid).
    /// 
    /// Returns null if no valid claim is found.
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
    // Helper: Audit Logger (WITH CancellationToken)
    // ============================================================
    /// <summary>
    /// Records an audit log entry for security and tracking role permission modifications.
    /// 
    /// Features:
    /// - Uses GetCurrentUserId() for reliable actor identification.
    /// - Accepts CancellationToken to stop immediately on client disconnect.
    /// - Stores OldValues and NewValues as JSON.
    /// </summary>
    private async Task Audit(
        string action,
        Guid? uid,
        string? eid,
        object? oldV = null,
        object? newV = null,
        CancellationToken cancellationToken = default)
    {
        var createdBy = GetCurrentUserId();

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = uid,
            Action = action,
            Entity = "role_permissions",
            EntityId = eid,
            TableName = "role_permissions",
            OldValues = oldV != null ? JsonSerializer.Serialize(oldV) : null,
            NewValues = newV != null ? JsonSerializer.Serialize(newV) : null,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            CreatedBy = createdBy,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ============================================================
    // Helper: Revoke Tokens for Role Users (WITH CancellationToken)
    // ============================================================
    /// <summary>
    /// Revokes active tokens for all users assigned to a specific role.
    /// Forces users to re-login and get an updated JWT.
    /// </summary>
    private async Task RevokeTokensForRoleUsersAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var currentUserId = GetCurrentUserId()?.ToString();

        var userIds = await _db.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await _tokenService.RevokeAllUserTokensAsync(userId, remoteIp, currentUserId);
        }
    }

    // ============================================================
    // GET: /api/roles/{roleId}/permissions
    // ============================================================
    /// <summary>
    /// Retrieves all permissions assigned to a specific role.
    /// Supports search, pagination, and cancellation.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RoleRead")]
    public async Task<IActionResult> GetRolePermissions(
        Guid roleId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
                return NotFound(new { code = "ROLE_NOT_FOUND", message = "Role not found" });

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.RolePermissions
                .Where(rp => rp.RoleId == roleId && rp.Permission != null)
                .Select(rp => rp.Permission!)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.PermissionName.ToLower().Contains(s) ||
                    (p.Description != null && p.Description.ToLower().Contains(s)) ||
                    p.Module.ToLower().Contains(s)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var perms = await query
                .OrderBy(p => p.PermissionName)
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

            return Ok(new { items = perms, total, page, pageSize });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The role permissions request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for role {RoleId}", roleId);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }

    // ============================================================
    // POST: /api/roles/{roleId}/permissions
    // ============================================================
    /// <summary>
    /// Assigns a SINGLE permission to a role.
    /// 
    /// Enhanced Audit:
    /// - Records RoleName + PermissionName in NewValues.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> AssignPermission(
        Guid roleId,
        [FromBody] AssignPermissionRequest req,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate Role
            var role = await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

            if (role == null)
                return NotFound(new { code = "ROLE_NOT_FOUND", message = "Role not found" });

            // 2. Validate Permission
            var permission = await _db.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == req.PermissionId, cancellationToken);

            if (permission == null)
                return NotFound(new { code = "PERMISSION_NOT_FOUND", message = "Permission not found" });

            // 3. Check duplicate
            if (await _db.RolePermissions.AnyAsync(
                rp => rp.RoleId == roleId && rp.PermissionId == req.PermissionId,
                cancellationToken))
            {
                return Conflict(new { code = "PERMISSION_ALREADY_ASSIGNED", message = "Permission already assigned" });
            }

            // 4. Insert
            var rolePermission = new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = req.PermissionId,
                IsActive = true,
                GrantedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.RolePermissions.Add(rolePermission);
            await _db.SaveChangesAsync(cancellationToken);

            // 5. Revoke tokens
            await RevokeTokensForRoleUsersAsync(roleId, cancellationToken);

            // 6. Enhanced Audit
            await Audit(
                action: "ASSIGN_PERMISSION",
                uid: null,
                eid: roleId.ToString(),
                oldV: null,
                newV: new
                {
                    RoleName = role.RoleName,
                    PermissionId = permission.Id,
                    PermissionName = permission.PermissionName,
                    Module = permission.Module
                },
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "Permission {PermissionName} assigned to role {RoleName}",
                permission.PermissionName, role.RoleName);

            return Ok(new { message = "Permission assigned" });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The assign permission request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", req.PermissionId, roleId);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }

    // ============================================================
    // PUT: /api/roles/{roleId}/permissions (BULK SYNC with ENHANCED AUDIT)
    // ============================================================
    /// <summary>
    /// Synchronizes a role's permissions in bulk.
    /// 
    /// Features:
    /// - Transaction (Atomicity): ALL changes succeed or NONE.
    /// - Enhanced Audit: Records OLD list, NEW list, Removed, Added.
    /// - Revokes tokens for role users.
    /// 
    /// GOLDEN RULE #11: Bulk Operations.
    /// </summary>
    [HttpPut]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> BulkSync(
        Guid roleId,
        [FromBody] BulkSyncPermissionsRequest req,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate Role
            var role = await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

            if (role == null)
                return NotFound(new { code = "ROLE_NOT_FOUND", message = "Role not found" });

            var distinctPermIds = req.PermissionIds.Distinct().ToList();

            // 2. Validate all permission IDs exist
            var existingPermissions = await _db.Permissions
                .AsNoTracking()
                .Where(p => distinctPermIds.Contains(p.Id))
                .Select(p => new { p.Id, p.PermissionName, p.Module })
                .ToListAsync(cancellationToken);

            if (existingPermissions.Count != distinctPermIds.Count)
                return BadRequest(new { code = "PERMISSIONS_NOT_FOUND", message = "One or more permissions not found" });

            // ============================================================
            // 3. Capture OLD permissions with names (BEFORE deletion)
            // ============================================================
            var oldPermissions = await _db.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId && rp.Permission != null)
                .Select(rp => new
                {
                    Id = rp.PermissionId,
                    Name = rp.Permission!.PermissionName,
                    Module = rp.Permission.Module
                })
                .ToListAsync(cancellationToken);

            // ============================================================
            // 4. Transaction: Remove + Add
            // ============================================================
            await using var transaction = await _db.Database
                .BeginTransactionAsync(cancellationToken);

            try
            {
                // 4.1. Remove all current permissions
                var current = await _db.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync(cancellationToken);

                _db.RolePermissions.RemoveRange(current);

                // 4.2. Add new permissions
                foreach (var pid in distinctPermIds)
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        PermissionId = pid,
                        IsActive = true,
                        GrantedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync(cancellationToken);

                // 4.3. Commit
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                // Rollback on any failure
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning("BulkSync transaction rolled back for role {RoleId}", roleId);
                throw;
            }

            // ============================================================
            // 5. Capture NEW permissions with names (AFTER insertion)
            // ============================================================
            var newPermissions = await _db.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId && rp.Permission != null)
                .Select(rp => new
                {
                    Id = rp.PermissionId,
                    Name = rp.Permission!.PermissionName,
                    Module = rp.Permission.Module
                })
                .ToListAsync(cancellationToken);

            // ============================================================
            // 6. Calculate DIFF (Removed / Added)
            // ============================================================
            var oldIds = oldPermissions.Select(p => p.Id).ToHashSet();
            var newIds = newPermissions.Select(p => p.Id).ToHashSet();

            var removedPermissions = oldPermissions
                .Where(p => !newIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name, p.Module })
                .ToList();

            var addedPermissions = newPermissions
                .Where(p => !oldIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name, p.Module })
                .ToList();

            // ============================================================
            // 7. Revoke tokens (outside transaction)
            // ============================================================
            await RevokeTokensForRoleUsersAsync(roleId, cancellationToken);

            // ============================================================
            // 8. Enhanced Audit Log (OLD + NEW + DIFF)
            // ============================================================
            await Audit(
                action: "BULK_SYNC_PERMISSIONS",
                uid: null,
                eid: roleId.ToString(),
                oldV: new
                {
                    RoleName = role.RoleName,
                    PermissionCount = oldPermissions.Count,
                    Permissions = oldPermissions.Select(p => new { p.Id, p.Name, p.Module }).ToList()
                },
                newV: new
                {
                    RoleName = role.RoleName,
                    PermissionCount = newPermissions.Count,
                    Permissions = newPermissions.Select(p => new { p.Id, p.Name, p.Module }).ToList(),
                    Added = addedPermissions,
                    Removed = removedPermissions
                },
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "Permissions bulk synced for role {RoleName}. Added: {AddedCount}, Removed: {RemovedCount}",
                role.RoleName, addedPermissions.Count, removedPermissions.Count);

            // ============================================================
            // 9. Return Result (with DIFF for UI)
            // ============================================================
            return Ok(new
            {
                message = "Permissions synced",
                count = distinctPermIds.Count,
                added = addedPermissions.Select(p => p.Name).ToList(),
                removed = removedPermissions.Select(p => p.Name).ToList()
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The bulk sync permissions request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing permissions for role {RoleId}", roleId);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }

    // ============================================================
    // DELETE: /api/roles/{roleId}/permissions/{permissionId}
    // ============================================================
    /// <summary>
    /// Removes a SINGLE permission from a role.
    /// 
    /// Enhanced Audit:
    /// - Records RoleName + PermissionName in OldValues.
    /// </summary>
    [HttpDelete("{permissionId}")]
    [Authorize(Policy = "RoleWrite")]
    public async Task<IActionResult> RemovePermission(
        Guid roleId,
        Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate Role
            var role = await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

            if (role == null)
                return NotFound(new { code = "ROLE_NOT_FOUND", message = "Role not found" });

            // 2. Find RolePermission
            var rp = await _db.RolePermissions
                .Include(x => x.Permission)
                .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, cancellationToken);

            if (rp == null)
                return NotFound(new { code = "ROLE_PERMISSION_NOT_FOUND", message = "Role permission assignment not found" });

            // 3. Capture details BEFORE deletion
            var removedPermissionInfo = new
            {
                RoleName = role.RoleName,
                PermissionId = rp.PermissionId,
                PermissionName = rp.Permission?.PermissionName,
                Module = rp.Permission?.Module
            };

            // 4. Delete
            _db.RolePermissions.Remove(rp);
            await _db.SaveChangesAsync(cancellationToken);

            // 5. Revoke tokens
            await RevokeTokensForRoleUsersAsync(roleId, cancellationToken);

            // 6. Enhanced Audit
            await Audit(
                action: "REMOVE_PERMISSION",
                uid: null,
                eid: roleId.ToString(),
                oldV: removedPermissionInfo,
                newV: null,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "Permission {PermissionName} removed from role {RoleName}",
                removedPermissionInfo.PermissionName, role.RoleName);

            return NoContent();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The remove permission request was canceled by the client.");
            return StatusCode(499, new { code = "REQUEST_CANCELED", message = "Request canceled by client." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", permissionId, roleId);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }
}