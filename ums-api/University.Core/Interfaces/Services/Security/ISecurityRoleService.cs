using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Core.Interfaces.Services.Security;

public interface ISecurityRoleService
{
    Task<Result<IEnumerable<SecurityRoleDto>>> GetAllAsync(string? branchCode = null);
    Task<Result<SecurityRoleDto>> GetByIdAsync(Guid id);
    Task<Result<SecurityRoleDto>> CreateAsync(CreateSecurityRoleDto dto);
    Task<Result<SecurityRoleDto>> UpdateAsync(Guid id, UpdateSecurityRoleDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
    Task<Result<IEnumerable<Guid>>> GetPermissionIdsAsync(Guid id);
    Task<Result<bool>> SetPermissionsAsync(Guid id, AssignPermissionsDto dto);
}
