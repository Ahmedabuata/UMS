namespace Identity.Api.DTOs;

/// <summary>
/// Query parameters for filtering audit logs.
/// </summary>
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

/// <summary>
/// Data Transfer Object for Audit Log.
/// 
/// IMPORTANT: EntityId vs EntityIdString
/// ─────────────────────────────────────
/// - EntityIdString: Used in DB projection (translatable to SQL).
/// - EntityId:       Computed in-memory from EntityIdString (NOT in projection).
/// 
/// This separation prevents EF Core from attempting to translate
/// 'Guid.Parse(a.EntityId)' which would trigger Client Evaluation
/// (loading ALL records into memory instead of just 20).
/// 
/// NOTE: UserName and CreatedByName are NO LONGER optional.
/// This avoids EF Core's "Expression tree may not contain optional arguments" error.
/// Always pass null in projection and populate later via EnrichWithUserNamesAsync.
/// </summary>
public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string? Entity,
    Guid? EntityId,                    // Computed in-memory
    string? EntityIdString,            // Raw from DB (DB-translatable)
    string? TableName,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    string? UserAgent,
    string? BranchCode,
    Guid? CreatedBy,
    DateTime Timestamp,
    DateTime CreatedAt,
    string? UserName,                  // NO default (= null removed)
    string? CreatedByName              // NO default (= null removed)
);

/// <summary>
/// Paginated response for audit logs list.
/// </summary>
public record AuditLogListResponse(
    List<AuditLogDto> Items,
    int Total,
    int Page,
    int PageSize
);