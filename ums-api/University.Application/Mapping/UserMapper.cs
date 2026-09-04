using University.Core.Entities;
using University.Shared.DTOs.Users;

namespace University.Application.Mapping;

public static class UserMapper
{
    public static UserResponseDto ToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        RoleName = user.Role?.RoleName ?? string.Empty,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}