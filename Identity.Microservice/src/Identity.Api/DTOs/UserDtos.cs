namespace Identity.Api.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool MustChangePassword,
    int FailedLoginAttempts,
    DateTime? LockoutEnd,
    string? FirstName,
    string? LastName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Roles,
    List<string>? Permissions = null,
    List<string>? Groups = null,
    UserProfileDto? Profile = null 
);

public record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    string? RoleName = null
);

public record UpdateUserRequest(
    string? Username,
    string? Email,
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    bool? IsActive = null
);

public record PatchUserStatusRequest(bool IsActive);
public record PatchUserPasswordRequest(string CurrentPassword, string NewPassword);
public record PatchUserLockoutRequest(DateTime? LockoutEnd);
public record UserListResponse(List<UserDto> Items, int Total, int Page, int PageSize);