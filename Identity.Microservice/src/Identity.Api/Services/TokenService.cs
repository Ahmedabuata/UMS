using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Api.Data;
using Identity.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Api.Services;

public interface ITokenService
{
    Task<(string accessToken, DateTime expiresAt)> GenerateAccessTokenAsync(User user);
    Task<(RefreshToken entity, string rawToken)> GenerateRefreshTokenAsync(Guid userId, string ip, string userAgent);
    Task<(RefreshToken entity, string rawToken)> RotateRefreshTokenAsync(RefreshToken oldToken, string ip, string userAgent);
    string HashToken(string token);
    Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null);
    Task<bool> ValidateRefreshTokenAsync(string rawToken, Guid userId, string? currentIp = null, string? currentUserAgent = null);
    Task<RefreshToken?> GetRefreshTokenAsync(string rawToken);
}

public class TokenService : ITokenService
{
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;

    public TokenService(IdentityDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(string accessToken, DateTime expiresAt)> GenerateAccessTokenAsync(User user)
    {
        var userRoles = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .ToListAsync();

        var roles = userRoles.Select(ur => ur.Role!.RoleName).ToList();
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

        var permissions = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission!.PermissionName)
            .Distinct()
            .ToListAsync();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "15"));

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim("userId", user.Id.ToString()),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public async Task<(RefreshToken entity, string rawToken)> GenerateRefreshTokenAsync(Guid userId, string ip, string userAgent)
    {
        var rawToken = GenerateRandomToken();
        var tokenHash = HashToken(rawToken);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            IpAddress = ip,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"] ?? "7")),
            Revoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return (refreshToken, rawToken);
    }

    public async Task<(RefreshToken entity, string rawToken)> RotateRefreshTokenAsync(RefreshToken oldToken, string ip, string userAgent)
    {
        // 🚨 1. Reuse Detection
        if (oldToken.Revoked)
        {
            await RevokeAllUserTokensAsync(oldToken.UserId, ip);
            throw new SecurityTokenException("Token reuse detected - possible theft. All user sessions revoked.");
        }

        // 2. Revoke old token
        oldToken.Revoked = true;
        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.RevokedByIp = ip;

        // 3. Generate new token
        var rawNewToken = GenerateRandomToken();
        var newTokenHash = HashToken(rawNewToken);

        oldToken.ReplacedByToken = newTokenHash;

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = oldToken.UserId,
            TokenHash = newTokenHash,
            IpAddress = ip,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"] ?? "7")),
            Revoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();

        return (newToken, rawNewToken);
    }

    public string HashToken(string token)
    {
        var pepper = _config["Jwt:RefreshTokenPepper"] ?? _config["Jwt:Key"]!;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(token)));
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null)
    {
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.Revoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Revoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
                .SetProperty(t => t.RevokedByIp, revokedByIp));
    }

    public async Task<bool> ValidateRefreshTokenAsync(string rawToken, Guid userId, string? currentIp = null, string? currentUserAgent = null)
    {
        var tokenHash = HashToken(rawToken);

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId && !t.Revoked);

        if (token == null || token.ExpiresAt < DateTime.UtcNow)
            return false;

        return true;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string rawToken)
    {
        var tokenHash = HashToken(rawToken);

        return await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.Revoked && t.ExpiresAt > DateTime.UtcNow);
    }

    private string GenerateRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}