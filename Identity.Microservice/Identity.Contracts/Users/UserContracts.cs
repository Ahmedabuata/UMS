namespace Identity.Contracts.Users;

public record CreateUserRequest(
    string Username,
    string? Email,
    string? PhoneNumber,
    string Password,
    List<Guid> RoleIds,
    List<Guid>? GroupIds,
    string? BranchCode
);

public record UpdateUserRequest(
    string? Email,
    string? PhoneNumber,
    bool? IsActive,
    List<Guid>? RoleIds,
    List<Guid>? GroupIds
);

public record UserDto(
    Guid Id,
    string Username,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    bool MustChangePassword,
    List<string> Roles,
    List<string> Permissions,
    DateTime CreatedAt
);
