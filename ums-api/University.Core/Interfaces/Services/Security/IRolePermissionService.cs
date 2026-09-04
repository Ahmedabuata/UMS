using University.Shared.Common;

namespace University.Core.Interfaces.Services.Security;

public interface IRolePermissionService
{
    Task<Result<bool>> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<Result<bool>> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<Result<IReadOnlyList<Guid>>> GetRolePermissionsAsync(Guid roleId);
}
