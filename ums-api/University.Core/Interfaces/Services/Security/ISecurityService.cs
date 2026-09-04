using University.Shared.Common;

namespace University.Core.Interfaces.Services.Security;

public interface ISecurityService
{
    Task<Result<Guid?>> GetCurrentUserIdAsync();
    Task<Result<bool>> IsInRoleAsync(string role);
}
