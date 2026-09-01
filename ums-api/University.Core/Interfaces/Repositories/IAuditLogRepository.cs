using System.Linq.Expressions;
using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task<AuditLog?> GetByIdAsync(Guid id);
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<IEnumerable<AuditLog>> FindAsync(Expression<Func<AuditLog, bool>> predicate);
    Task<AuditLog> AddAsync(AuditLog entity);
    Task<IEnumerable<AuditLog>> AddRangeAsync(IEnumerable<AuditLog> entities);
    Task<int> CountAsync(Expression<Func<AuditLog, bool>>? predicate = null);
}
