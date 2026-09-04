using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetWithRoleAsync(Guid id);
}
