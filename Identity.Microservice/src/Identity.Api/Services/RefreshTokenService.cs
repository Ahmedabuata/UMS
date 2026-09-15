using Identity.Api.Data;
using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Identity.Api.Interfaces; 
namespace Identity.Api.Services;

/// <summary>
/// Implements refresh token management operations.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IdentityDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IdentityDbContext db,
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<RefreshTokenService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<RefreshToken>> GetUserActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.Revoked && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(List<RefreshToken> Tokens, int Total)> GetUserTokensPaginatedAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.RefreshTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(cancellationToken);

        var tokens = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (tokens, total);
    }

    /// <inheritdoc />
    public async Task<int> GetUserActiveTokenCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.RefreshTokens
            .CountAsync(t => t.UserId == userId && !t.Revoked && t.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null, string? revokedBy = null, CancellationToken cancellationToken = default)
    {
        await _tokenService.RevokeAllUserTokensAsync(userId, revokedByIp, revokedBy, cancellationToken);

        await _auditService.LogAsync(
            action: "REVOKE_ALL_TOKENS",
            userId: userId,
            entityId: userId.ToString(),
            entity: "RefreshToken",
            tableName: "refresh_tokens",
            ipAddress: revokedByIp,
            createdBy: Guid.TryParse(revokedBy, out var parsedId) ? parsedId : userId,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("All tokens revoked for user {UserId}", userId);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeTokenByHashAsync(string tokenHash, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.Revoked, cancellationToken);

        if (token == null)
            return false;

        token.Revoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = revokedByIp;

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            action: "REVOKE_TOKEN",
            userId: token.UserId,
            entityId: token.Id.ToString(),
            entity: "RefreshToken",
            tableName: "refresh_tokens",
            ipAddress: revokedByIp,
            createdBy: token.UserId,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Token {TokenId} revoked for user {UserId}", token.Id, token.UserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeTokenAsync(string rawToken, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(rawToken);
        return await RevokeTokenByHashAsync(tokenHash, revokedByIp, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.Revoked && t.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetTokenAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(rawToken);
        return await GetTokenByHashAsync(tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CleanExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        return await _tokenService.CleanExpiredTokensAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsTokenValidAsync(string rawToken, Guid userId, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(rawToken);

        return await _db.RefreshTokens
            .AnyAsync(t =>
                t.TokenHash == tokenHash &&
                t.UserId == userId &&
                !t.Revoked &&
                t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(bool IsValid, RefreshToken? Token)> ValidateTokenAsync(
        string rawToken,
        Guid userId,
        string? currentIp = null,
        string? currentUserAgent = null,
        CancellationToken cancellationToken = default)
    {
        return await _tokenService.ValidateRefreshTokenAsync(rawToken, userId, currentIp, currentUserAgent, cancellationToken);
    }
}