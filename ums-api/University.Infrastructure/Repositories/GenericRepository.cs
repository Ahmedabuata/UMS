using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.Interfaces.Repositories;

namespace University.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id) =>
        await _dbSet.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync() =>
        await _dbSet.Where(e => e.IsActive).ToListAsync();

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.Where(predicate).Where(e => e.IsActive).ToListAsync();

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        var list = entities.ToList();
        await _dbSet.AddRangeAsync(list);
        return list;
    }

    public virtual Task<T> UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null)
        {
            return;
        }

        // Soft delete
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var query = _dbSet.Where(e => e.IsActive);
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync();
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.Where(e => e.IsActive).AnyAsync(predicate);

    public virtual async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
