using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Core.Interfaces.Services.Security;

public interface ISecurityPermissionQueryService
{
    Task<Result<IEnumerable<PermissionItemDto>>> GetAllAsync(string? module = null);
    Task<Result<IEnumerable<PermissionByModuleDto>>> GetGroupedByModuleAsync();
    Task<Result<IEnumerable<ModuleItemDto>>> GetModulesAsync();
}
