using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;

namespace University.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _dbSet.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _dbSet.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<bool> EmailExistsAsync(string email) =>
        await _dbSet.AnyAsync(u => u.Email == email);

    public async Task<User?> GetWithRoleAsync(Guid id) =>
        await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<IEnumerable<User>> GetByBranchAsync(Guid branchId) =>
        await _dbSet.Where(u => u.BranchId == branchId).ToListAsync();
}
