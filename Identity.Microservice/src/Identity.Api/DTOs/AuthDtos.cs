namespace Identity.Api.DTOs;

/// <summary>
/// Represents the registration request payload.
/// </summary>
public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? PhoneNumber,
    string? FirstName = null,
    string? LastName = null
);

/// <summary>
/// Represents the login request payload.
/// </summary>
public record LoginRequest(string EmailOrUsername, string Password);

/// <summary>
/// Represents the refresh token request payload.
/// </summary>
public record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Represents the token revocation request payload.
/// </summary>
public record RevokeTokenRequest(string? RefreshToken);

/// <summary>
/// Represents the authentication response containing tokens and user data.
/// </summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    UserDto User
);

/// <summary>
/// Represents the response containing current user data.
/// </summary>
public record MeResponse(UserDto User);

/// <summary>
/// Represents the password change request payload.
/// </summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);