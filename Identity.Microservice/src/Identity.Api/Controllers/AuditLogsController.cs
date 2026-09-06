using Identity.Api.Data;
using Identity.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    public AuditLogsController(IdentityDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "AuditRead")]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from, 
        [FromQuery] DateTime? to, 
        [FromQuery] Guid? userId, 
        [FromQuery] string? action, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page); 
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.AuditLogs.AsQueryable();

        if (from != null) q = q.Where(a => a.CreatedAt >= from);
        if (to != null) q = q.Where(a => a.CreatedAt <= to);
        if (userId != null) q = q.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action == action.Trim());

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.CreatedAt)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .Select(a => new AuditLogDto(
                               a.Id, 
                               a.UserId, 
                               a.Action, 
                               a.EntityName, 
                               a.EntityId, 
                               null, 
                               a.OldValues, 
                               a.NewValues, 
                               a.IpAddress, 
                               null, 
                               null, 
                               null, 
                               null, 
                               a.CreatedAt))
                           .ToListAsync();

        return Ok(new AuditLogListResponse(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AuditRead")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var a = await _db.AuditLogs.FindAsync(id); 
        if (a == null) return NotFound();
        return Ok(new AuditLogDto(
            a.Id, 
            a.UserId, 
            a.Action, 
            a.EntityName, 
            a.EntityId, 
            null, 
            a.OldValues, 
            a.NewValues, 
            a.IpAddress, 
            null, 
            null, 
            null, 
            null, 
            a.CreatedAt));
    }
}