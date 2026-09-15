namespace Identity.Api.DTOs;

public record CreateGroupRequest(
    string Name,
    string DisplayName,
    string? Description = null,
    string? BranchCode = null
)
{
    public string GroupName => Name;
}

public record UpdateGroupRequest(
    string? Name = null,
    string? DisplayName = null,
    string? Description = null,
    string? BranchCode = null,
    bool? IsActive = null
)
{
    public string? GroupName => Name;
}

public record GroupDto(
    Guid Id,
    string Name,
    string DisplayName,
    string? Description,
    string? BranchCode,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);