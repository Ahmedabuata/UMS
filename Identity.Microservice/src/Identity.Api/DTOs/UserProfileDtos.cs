namespace Identity.Api.DTOs;

public record UserProfileDto(
    Guid Id,
    Guid UserId,
    string? Address,
    string? City,
    string? PostalCode,
    string? MaritalStatus,
    DateTime? DateOfBirth,
    string? Gender,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UpsertUserProfileRequest(
    string? Address,
    string? City,
    string? PostalCode,
    string? MaritalStatus,
    DateTime? DateOfBirth,
    string? Gender
);