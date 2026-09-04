using University.Shared.Common;
using University.Shared.DTOs.Users;

namespace University.Core.Interfaces.Services;

public interface IUserService
{
    Task<Result<UserResponseDto>> GetUserByIdAsync(Guid id);
    Task<Result<UserResponseDto>> CreateUserAsync(CreateUserRequestDto dto);
    Task<Result<UserResponseDto>> UpdateUserAsync(Guid id, UpdateUserRequestDto dto);
    Task<Result<bool>> DeleteUserAsync(Guid id);
    Task<Result<bool>> AssignRoleAsync(Guid userId, Guid roleId);
}
