namespace Identity.Api.DTOs;

/// <summary>
/// Request for creating a new user.
/// 
/// Password rules:
/// - If SendPasswordByEmail = true  → Password must be null/empty (backend generates random).
/// - If SendPasswordByEmail = false → Password is REQUIRED (admin provides).
/// </summary>
public record CreateUserRequestDto(
    string Username,
    string Email,
    string? Password,                 // Optional now (null when SendPasswordByEmail = true)
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    string? RoleName = null,
    bool SendPasswordByEmail = false  // NEW: Generate + email password
);