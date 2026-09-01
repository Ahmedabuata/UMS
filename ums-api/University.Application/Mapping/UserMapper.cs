using University.Core.Entities;
using University.Shared.DTOs.Users;

namespace University.Application.Mapping;

public static class UserMapper
{
    public static UserResponseDto ToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        BranchId = user.BranchId,
        BranchName = user.Branch?.BranchName,
        RoleId = user.RoleId,
        RoleName = user.Role?.RoleName ?? string.Empty,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
