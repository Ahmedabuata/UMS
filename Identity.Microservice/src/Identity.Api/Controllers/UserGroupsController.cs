using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/groups")]
[Authorize]
public class UserGroupsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    public UserGroupsController(IdentityDbContext db) => _db = db;

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

        _db.UserGroups.Add(new UserGroup { UserId = userId, GroupId = req.GroupId });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Group assigned" });
    }

    [HttpDelete("{groupId}")]
    [Authorize(Policy = "GroupWrite")]
    public async Task<IActionResult> RemoveGroup(Guid userId, Guid groupId)
    {
        var ug = await _db.UserGroups.FirstOrDefaultAsync(x => x.UserId == userId && x.GroupId == groupId);
        if (ug == null) return NotFound();

        _db.UserGroups.Remove(ug);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}