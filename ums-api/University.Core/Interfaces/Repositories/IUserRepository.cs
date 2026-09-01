using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetWithRoleAsync(Guid id);
    Task<IEnumerable<User>> GetByBranchAsync(Guid branchId);
}
