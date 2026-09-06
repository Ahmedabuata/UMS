namespace Identity.Api.DTOs;

public record CreateRoleRequest(
    string RoleName,
    string? DisplayName = null,
    string? Description = null,
    string? BranchCode = null,
    bool IsSystemRole = false
);

public record UpdateRoleRequest(
    string? DisplayName = null,
    string? Description = null,
    string? BranchCode = null,
    bool? IsActive = null
);

public record RoleDto(
    Guid Id,
    string RoleName,
    string? DisplayName = null,
    string? Description = null,
    string? BranchCode = null,
    bool IsSystemRole = false,
    bool IsActive = true,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default,
    int UserCount = 0
);