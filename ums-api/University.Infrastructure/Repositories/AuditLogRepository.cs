using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;

namespace University.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id) =>
        await _context.AuditLogs.FindAsync(id);

    public async Task<IEnumerable<AuditLog>> GetAllAsync() =>
        await _context.AuditLogs.OrderByDescending(e => e.Timestamp).ToListAsync();

    public async Task<IEnumerable<AuditLog>> FindAsync(Expression<Func<AuditLog, bool>> predicate) =>
        await _context.AuditLogs.Where(predicate).ToListAsync();

    public async Task<AuditLog> AddAsync(AuditLog entity)
    {
        await _context.AuditLogs.AddAsync(entity);
        return entity;
    }

    public async Task<IEnumerable<AuditLog>> AddRangeAsync(IEnumerable<AuditLog> entities)
    {
        var list = entities.ToList();
        await _context.AuditLogs.AddRangeAsync(list);
        return list;
    }

    public async Task<int> CountAsync(Expression<Func<AuditLog, bool>>? predicate = null)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync();
    }
}
