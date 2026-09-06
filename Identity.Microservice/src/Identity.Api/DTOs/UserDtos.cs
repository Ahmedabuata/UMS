namespace Identity.Api.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool MustChangePassword,
    DateTime CreatedAt,
    List<string> Roles,
    List<string>? Permissions = null,
    List<string>? Groups = null
);

public record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string? PhoneNumber,
    string? RoleName = null
);

public record UpdateUserRequest(
    string? Username,
    string? Email,
    string? PhoneNumber
);

public record PatchUserStatusRequest(bool IsActive);
public record UserListResponse(List<UserDto> Items, int Total, int Page, int PageSize);
