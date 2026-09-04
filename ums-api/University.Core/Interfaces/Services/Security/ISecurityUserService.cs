using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Core.Interfaces.Services.Security;

public interface ISecurityUserService
{
    Task<Result<PagedResult<SecurityUserDto>>> SearchAsync(
        string? search = null, string? branchCode = null, string? userType = null,
        bool? active = null, int page = 1, int pageSize = 20, bool? mustChangePwd = null);
    Task<Result<SecurityUserDto>> GetByIdAsync(Guid id);
    Task<Result<SecurityUserDto>> CreateAsync(CreateSecurityUserDto dto);
    Task<Result<SecurityUserDto>> CreateForEmployeeAsync(Guid employeeId, CreateUserForEmployeeDto dto);
    Task<Result<SecurityUserDto>> UpdateAsync(Guid id, UpdateSecurityUserDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
    Task<Result<bool>> ActivateAsync(Guid id, bool activate);
    Task<Result<bool>> UnlockAsync(Guid id);
    Task<Result<bool>> ResetPasswordAsync(Guid id, string newPassword);
    Task<Result<bool>> SetRolesAsync(Guid id, SetUserRolesDto dto);
    Task<Result<IEnumerable<SecurityRoleDto>>> GetUserRolesAsync(Guid id);
    Task<Result<IEnumerable<SecurityGroupDto>>> GetUserGroupsAsync(Guid id);
    Task<Result<bool>> AddToGroupAsync(Guid id, Guid groupId);
    Task<Result<bool>> RemoveFromGroupAsync(Guid id, Guid groupId);
}
