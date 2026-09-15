using Identity.Api.Models;

namespace Identity.Api.Interfaces;
/// <summary>
/// Defines the contract for refresh token management operations.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Retrieves all active refresh tokens for a user.
    /// </summary>
    Task<List<RefreshToken>> GetUserActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated refresh tokens for a user.
    /// </summary>
    Task<(List<RefreshToken> Tokens, int Total)> GetUserTokensPaginatedAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active refresh tokens for a user.
    /// </summary>
    Task<int> GetUserActiveTokenCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active tokens for a user.
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null, string? revokedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific token by its hash.
    /// </summary>
    Task<bool> RevokeTokenByHashAsync(string tokenHash, string? revokedByIp = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific token by its raw value.
    /// </summary>
    Task<bool> RevokeTokenAsync(string rawToken, string? revokedByIp = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a token by its hash.
    /// </summary>
    Task<RefreshToken?> GetTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a token by its raw value.
    /// </summary>
    Task<RefreshToken?> GetTokenAsync(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired and revoked tokens older than 30 days.
    /// </summary>
    Task<int> CleanExpiredTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is valid (not revoked and not expired).
    /// </summary>
    Task<bool> IsTokenValidAsync(string rawToken, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a token with subnet matching.
    /// </summary>
    Task<(bool IsValid, RefreshToken? Token)> ValidateTokenAsync(
        string rawToken,
        Guid userId,
        string? currentIp = null,
        string? currentUserAgent = null,
        CancellationToken cancellationToken = default);
}