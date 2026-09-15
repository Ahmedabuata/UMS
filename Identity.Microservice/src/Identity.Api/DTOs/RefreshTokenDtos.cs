namespace Identity.Api.DTOs;

public record RefreshTokenDto(
    Guid Id,
    Guid UserId,
    string? IpAddress,
    string? UserAgent,
    DateTime ExpiresAt,
    bool IsRevoked,
    DateTime? RevokedAt,
    string? RevokedByIp,
    DateTime CreatedAt
);

public record UserRefreshTokensResponse(
    List<RefreshTokenDto> Tokens,
    int Total,
    int Page,
    int PageSize
);

public record RevokeAllUserTokensRequest(
    Guid UserId
);