using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> LogAsync(string action, Guid? userId, string details)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            UserId = userId,
            Entity = "Security",
            NewValues = details,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<PagedResult<AuditLogItemDto>>> QueryAsync(
        string? search = null,
        string? action = null,
        string? entity = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _context.AuditLogs.AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(log => log.Timestamp >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(log => log.Timestamp <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var a = action.Trim();
            query = query.Where(log => log.Action != null && log.Action.Contains(a));
        }
        if (!string.IsNullOrWhiteSpace(entity))
        {
            var e = entity.Trim();
            query = query.Where(log => log.Entity != null && log.Entity.Contains(e));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(log =>
                (log.User != null && log.User.Username != null && log.User.Username.Contains(s)) ||
                (log.Action != null && log.Action.Contains(s)) ||
                (log.Entity != null && log.Entity.Contains(s)) ||
                (log.IpAddress != null && log.IpAddress.Contains(s)));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var items = await query
            .OrderByDescending(log => log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AuditLogItemDto
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                Action = log.Action,
                Entity = log.Entity,
                EntityId = log.EntityId,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                BranchCode = log.BranchCode,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                UserId = log.UserId,
                UserName = log.User != null ? log.User.Username : null
            })
            .ToListAsync();

        return Result<PagedResult<AuditLogItemDto>>.Success(new PagedResult<AuditLogItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages,
            Items = items
        });
    }
}