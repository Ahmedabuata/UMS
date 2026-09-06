using Identity.Api.Data;
using Identity.Api.DTOs;
using Identity.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    public GroupsController(IdentityDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "GroupRead")]
    public async Task<IActionResult> GetAll() 
        => Ok(await _db.Groups.OrderBy(g => g.Name)
            .Select(g => new GroupDto(g.Id, g.Name, null, g.Description, null, true, g.CreatedAt, g.CreatedAt))
            .ToListAsync());

    [HttpGet("{id}")]
    [Authorize(Policy = "GroupRead")]
    public async Task<IActionResult> GetById(Guid id)
    { 
        var g = await _db.Groups.FindAsync(id); 
        if (g == null) return NotFound(); 
        return Ok(new GroupDto(g.Id, g.Name, null, g.Description, null, true, g.CreatedAt, g.CreatedAt)); 
    }

    [HttpPost]
    [Authorize(Policy = "GroupWrite")]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name required" });
        if (await _db.Groups.AnyAsync(x => x.Name == req.Name.Trim())) return Conflict(new { message = "Group exists" });
        
        var g = new Group { Id = Guid.NewGuid(), Name = req.Name.Trim(), Description = req.Description?.Trim(), CreatedAt = DateTime.UtcNow };
        _db.Groups.Add(g); 
        await _db.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetById), new { id = g.Id }, new GroupDto(g.Id, g.Name, null, g.Description, null, true, g.CreatedAt, g.CreatedAt));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "GroupWrite")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupRequest req)
    {
        var g = await _db.Groups.FindAsync(id); 
        if (g == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Name)) g.Name = req.Name.Trim(); 
        g.Description = req.Description?.Trim(); 
        await _db.SaveChangesAsync();
        return Ok(new GroupDto(g.Id, g.Name, null, g.Description, null, true, g.CreatedAt, DateTime.UtcNow));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "GroupDelete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var g = await _db.Groups.FindAsync(id); 
        if (g == null) return NotFound();
        if (await _db.UserGroups.AnyAsync(ug => ug.GroupId == id)) return BadRequest(new { message = "Group has members. Remove members first." });
        
        _db.Groups.Remove(g); 
        await _db.SaveChangesAsync(); 
        return NoContent();
    }
}
