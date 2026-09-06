namespace Identity.Api.DTOs;

public record AuditLogQuery(
    DateTime? From = null,
    DateTime? To = null,
    Guid? UserId = null,
    string? Action = null,
    string? Entity = null,
    string? BranchCode = null,
    int Page = 1,
    int PageSize = 20
);

public record AuditLogDto(
    Guid Id,
    Guid? UserId = null,
    string? Action = null,
    string? Entity = null,
    string? EntityId = null,
    string? TableName = null,
    string? OldValues = null,
    string? NewValues = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? BranchCode = null,
    Guid? CreatedBy = null,
    DateTime? Timestamp = null,
    DateTime CreatedAt = default
);

public record AuditLogListResponse(List<AuditLogDto> Items, int Total, int Page, int PageSize);