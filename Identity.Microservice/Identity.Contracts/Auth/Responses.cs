namespace Identity.Contracts.Auth;

// مطابق تماماً لـ Dashboard.jsx و AuthContext
public record UserInfoDto(
    Guid Id,
    string Username,
    string? Email,
    string? FullName,
    string? EmployeeNumber,
    string? IdentifierNumber,
    string Role, // role
    string RoleName, // roleName
    List<string> Permissions,
    bool MustChangePassword
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfoDto User
);
