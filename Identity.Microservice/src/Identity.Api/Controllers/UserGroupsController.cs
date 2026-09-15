using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Services;
using Identity.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/groups")]
[Authorize]
public class UserGroupsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;

    public UserGroupsController(IdentityDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    private async Task Audit(string action, Guid? uid, string? eid, object? oldV = null, object? newV = null)
    {
        var userIdClaim = User?.FindFirst("userId")?.Value;
        var createdBy = !string.IsNullOrEmpty(userIdClaim) ? Guid.Parse(userIdClaim) : (Guid?)null;

        _db.AuditLogs.Add(new AuditLog 
        { 
            Id = Guid.NewGuid(), 
            UserId = uid, 
            Action = action, 
            Entity = "user_groups", 
            EntityId = eid, 
            TableName = "user_groups",
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
    /// Retrieves all groups assigned to a specific user.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "GroupRead")]
    public async Task<IActionResult> GetUserGroups(Guid userId)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId)) 
            return NotFound(new { message = "User not found" });

        var groups = await _db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Include(ug => ug.Group)
            .Where(ug => ug.Group != null)
            .Select(ug => new GroupDto(
                ug.Group!.Id,
                ug.Group.Name,
                ug.Group.DisplayName,
                ug.Group.Description,
                ug.Group.BranchCode,
                ug.Group.IsActive,
                ug.Group.CreatedAt,
                ug.Group.UpdatedAt
            ))
            .ToListAsync();

        return Ok(groups);
    }

    /// <summary>
    /// Assigns a new group to a specified user, updates user metadata, and records an audit log entry.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "GroupWrite")]
    public async Task<IActionResult> AssignGroup(Guid userId, [FromBody] AssignGroupRequest req)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId)) 
            return NotFound(new { message = "User not found" });
        if (!await _db.Groups.AnyAsync(g => g.Id == req.GroupId)) 
            return NotFound(new { message = "Group not found" });
        if (await _db.UserGroups.AnyAsync(ug => ug.UserId == userId && ug.GroupId == req.GroupId)) 
            return Conflict(new { message = "User already in group" });

        var userGroup = new UserGroup 
        { 
            Id = Guid.NewGuid(), 
            UserId = userId, 
            GroupId = req.GroupId, 
            JoinedAt = DateTime.UtcNow  // ✅ استخدم JoinedAt بدلاً من AssignedAt
        };

        _db.UserGroups.Add(userGroup);

        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _db.Users.Update(user);
        }

        await _db.SaveChangesAsync();

        await Audit("ASSIGN_GROUP_TO_USER", userId, req.GroupId.ToString(), null, new { UserId = userId, GroupId = req.GroupId });

        return Ok(new { message = "Group assigned" });
    }

    /// <summary>
    /// Removes an assigned group from a specified user, revokes active tokens securely, updates user metadata, and records an audit log entry.
    /// </summary>
    [HttpDelete("{groupId}")]
    [Authorize(Policy = "GroupWrite")]
    public async Task<IActionResult> RemoveGroup(Guid userId, Guid groupId)
    {
        var ug = await _db.UserGroups.FirstOrDefaultAsync(x => x.UserId == userId && x.GroupId == groupId);
        if (ug == null) return NotFound();

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var currentUserId = User?.FindFirst("userId")?.Value;
        await _tokenService.RevokeAllUserTokensAsync(userId, remoteIp, currentUserId);

        _db.UserGroups.Remove(ug);

        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _db.Users.Update(user);
        }

        await _db.SaveChangesAsync();

        await Audit("REMOVE_GROUP_FROM_USER", userId, groupId.ToString(), new { UserId = userId, GroupId = groupId }, null);

        return NoContent();
    }
}