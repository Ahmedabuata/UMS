using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Core.Interfaces.Services.Security;

public interface ISecurityGroupService
{
    Task<Result<IEnumerable<SecurityGroupDto>>> GetAllAsync(string? branchCode = null);
    Task<Result<SecurityGroupDto>> GetByIdAsync(Guid id);
    Task<Result<SecurityGroupDto>> CreateAsync(CreateSecurityGroupDto dto);
    Task<Result<SecurityGroupDto>> UpdateAsync(Guid id, UpdateSecurityGroupDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
    Task<Result<IEnumerable<SecurityUserDto>>> GetMembersAsync(Guid id);
    Task<Result<bool>> AddMemberAsync(Guid id, Guid userId);
    Task<Result<bool>> RemoveMemberAsync(Guid id, Guid userId);
}
