using Identity.Api.Models;

namespace Identity.Api.Interfaces;

public interface ITokenService
{
    Task<(string accessToken, DateTime expiresAt)> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default);
    Task<(RefreshToken entity, string rawToken)> GenerateRefreshTokenAsync(Guid userId, string ip, string userAgent, CancellationToken cancellationToken = default);
    Task<(RefreshToken entity, string rawToken)> RotateRefreshTokenAsync(RefreshToken oldToken, string ip, string userAgent, CancellationToken cancellationToken = default);
    string HashToken(string token);
    Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null, string? revokedBy = null, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string rawToken, string? revokedByIp = null, CancellationToken cancellationToken = default);
    Task<(bool IsValid, RefreshToken? Token)> ValidateRefreshTokenAsync(string rawToken, Guid userId, string? currentIp = null, string? currentUserAgent = null, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenAsync(string rawToken, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetUserActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUserActiveTokenCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(List<RefreshToken> Tokens, int Total)> GetUserTokensPaginatedAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> CleanExpiredTokensAsync(CancellationToken cancellationToken = default);
}