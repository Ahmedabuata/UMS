using University.Shared.Common;
using University.Shared.DTOs.Permissions;

namespace University.Core.Interfaces.Services.Security;

public interface ISecurityPermissionService
{
    Task<Result<bool>> HasPermissionAsync(Guid userId, string permissionKey);
    Task<Result<IReadOnlyList<string>>> GetUserPermissionsAsync(Guid userId);
    Task<Result<PermissionResponseDto>> CreateAsync(CreatePermissionRequestDto dto);
    Task<Result<PermissionResponseDto>> UpdateAsync(Guid id, UpdatePermissionRequestDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}
