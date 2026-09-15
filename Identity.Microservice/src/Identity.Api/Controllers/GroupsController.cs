using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Identity.Api.Services;
using Identity.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IdentityDbContext db, ITokenService tokenService, ILogger<GroupsController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
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
            Entity = "groups", 
            EntityId = eid, 
            TableName = "groups",
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

    private async Task RevokeTokensForGroupUsersAsync(Guid groupId)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var currentUserId = User?.FindFirst("userId")?.Value;

        var userIds = await _db.UserGroups
            .Where(ug => ug.GroupId == groupId)
            .Select(ug => ug.UserId)
            .Distinct()
            .ToListAsync();

        if (!userIds.Any()) return;

        foreach (var userId in userIds)
        {
            await _tokenService.RevokeAllUserTokensAsync(userId, remoteIp, currentUserId);
        }
    }

    [HttpGet]
    [Authorize(Policy = "GroupRead")]
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

            var query = _db.Groups.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(g => 
                    g.Name.ToLower().Contains(s) ||
                    g.DisplayName.ToLower().Contains(s) ||
                    (g.Description != null && g.Description.ToLower().Contains(s))
                );
            }

            query = query.OrderBy(g => g.Name);
            var total = await query.CountAsync(cancellationToken);

            var groups = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new GroupDto(
                    g.Id, 
                    g.Name, 
                    g.DisplayName, 
                    g.Description, 
                    g.BranchCode, 
                    g.IsActive, 
                    g.CreatedAt, 
                    g.UpdatedAt  // 
                ))
                .ToListAsync(cancellationToken);

            return Ok(new { items = groups, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups list");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }
[HttpGet("count")]
    [Authorize(Policy = "GroupRead")]
    public async Task<IActionResult> GetCount(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _db.Groups.CountAsync(cancellationToken);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups count");
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }


    [HttpGet("{id}")]
    [Authorize(Policy = "GroupRead")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var g = await _db.Groups.FindAsync(new object[] { id }, cancellationToken); 
            if (g == null) return NotFound(new { code = "GROUP_NOT_FOUND", message = "Group not found" }); 

            return Ok(new GroupDto(
                g.Id, 
                g.Name, 
                g.DisplayName, 
                g.Description, 
                g.BranchCode, 
                g.IsActive, 
                g.CreatedAt, 
                g.UpdatedAt  // ✅ صحيح
            )); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group {GroupId}", id);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "GroupWrite")]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Name)) 
                return BadRequest(new { code = "GROUP_NAME_REQUIRED", message = "Name required" });

            var name = req.Name.Trim().ToUpperInvariant();
            if (await _db.Groups.AnyAsync(x => x.Name == name, cancellationToken)) 
                return Conflict(new { code = "DUPLICATE_GROUP", message = "Group exists" });

            var g = new Group 
            { 
                Id = Guid.NewGuid(), 
                Name = name, 
                DisplayName = req.DisplayName?.Trim() ?? name,
                Description = req.Description?.Trim(), 
                BranchCode = req.BranchCode,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Groups.Add(g); 
            await _db.SaveChangesAsync(cancellationToken);

            await Audit("CREATE_GROUP", null, g.Id.ToString(), null, new { g.Name, g.DisplayName });
            _logger.LogInformation("Group created: {GroupName}", g.Name);

            return CreatedAtAction(nameof(GetById), new { id = g.Id }, new GroupDto(
                g.Id, 
                g.Name, 
                g.DisplayName, 
                g.Description, 
                g.BranchCode, 
                g.IsActive, 
                g.CreatedAt, 
                g.UpdatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group {GroupName}", req?.Name);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "GroupWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var g = await _db.Groups.FindAsync(new object[] { id }, cancellationToken); 
            if (g == null) return NotFound(new { code = "GROUP_NOT_FOUND", message = "Group not found" });

            var oldValues = new { g.Name, g.DisplayName, g.Description, g.BranchCode, g.IsActive };

            if (req.DisplayName != null) 
                g.DisplayName = req.DisplayName.Trim();

            if (req.Description != null) 
                g.Description = req.Description.Trim();

            if (req.BranchCode != null) 
                g.BranchCode = req.BranchCode;

            if (req.IsActive.HasValue) 
                g.IsActive = req.IsActive.Value;

            g.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await RevokeTokensForGroupUsersAsync(id);
            await Audit("UPDATE_GROUP", null, id.ToString(), oldValues, new { g.Name, g.DisplayName, g.Description, g.BranchCode, g.IsActive });
            _logger.LogInformation("Group updated: {GroupId}", id);

            return Ok(new GroupDto(
                g.Id, 
                g.Name, 
                g.DisplayName, 
                g.Description, 
                g.BranchCode, 
                g.IsActive, 
                g.CreatedAt, 
                g.UpdatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", id);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "GroupDelete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var g = await _db.Groups.FindAsync(new object[] { id }, cancellationToken); 
            if (g == null) return NotFound(new { code = "GROUP_NOT_FOUND", message = "Group not found" });

            if (await _db.UserGroups.AnyAsync(ug => ug.GroupId == id, cancellationToken)) 
                return BadRequest(new { code = "GROUP_HAS_MEMBERS", message = "Group has members. Remove members first." });

            _db.Groups.Remove(g); 
            await _db.SaveChangesAsync(cancellationToken);

            await Audit("DELETE_GROUP", null, id.ToString(), new { g.Name }, null);
            _logger.LogInformation("Group deleted: {GroupName}", g.Name);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", id);
            return StatusCode(500, new { code = "INTERNAL_SERVER_ERROR", message = "An unexpected error occurred", error = ex.Message });
        }
    }
}