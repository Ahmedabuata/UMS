using University.Shared.Common;
using University.Shared.Configuration;

namespace University.Core.Interfaces.Services.Security;

public interface ISecurityPolicyService
{
    Task<Result<SecuritySettings>> GetAsync();
    Task<Result<bool>> UpdateAsync(SecuritySettings settings);
}
