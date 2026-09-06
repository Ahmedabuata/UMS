namespace Identity.Api.DTOs;

public record CreatePermissionRequest(
    string PermissionName,
    string? Description,
    string? Module = null,
    string? ModuleCode = null,
    string? BranchCode = null,
    bool IsSensitive = false
);

public record UpdatePermissionRequest(
    string? Description,
    string? Module,
    string? ModuleCode,
    string? BranchCode,
    bool? IsSensitive,
    bool? IsActive
);

public record PermissionDto(
    Guid Id,
    string PermissionName,
    string? Description = null,
    string? Module = null,
    string? ModuleCode = null,
    string? BranchCode = null,
    bool IsSensitive = false,
    bool IsActive = true,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default
);