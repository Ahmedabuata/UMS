using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Api.Data;
using Identity.Api.Models;
using Identity.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Api.Services;

public class TokenService : ITokenService
{
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;

    public TokenService(IdentityDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(string accessToken, DateTime expiresAt)> GenerateAccessTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        var userId = user.Id;

        var userRoles = await _db.UserRoles
         .Where(ur => ur.UserId == userId)
         .Include(ur => ur.Role)
         .Where(ur => ur.Role!= null && ur.Role.IsActive)
         .ToListAsync(cancellationToken);

        var roles = userRoles
         .Select(ur => ur.Role?.RoleName)
         .Where(r =>!string.IsNullOrWhiteSpace(r))
         .Select(r => r!)
         .ToList();

        // ✅ UserRole.RoleId = Guid (not nullable)
        var roleIds = userRoles
         .Select(ur => ur.RoleId)
         .Where(id => id!= Guid.Empty)
         .Distinct()
         .ToList();

        var permissionList = new List<string>();
        if (roleIds.Any())
        {
            // ✅ RolePermission.RoleId = Guid? (nullable) -> HasValue + Value
            permissionList = await _db.RolePermissions
             .Where(rp => rp.RoleId.HasValue && roleIds.Contains(rp.RoleId.Value))
             .Include(rp => rp.Permission)
             .Where(rp => rp.Permission!= null
                          && rp.Permission.PermissionName!= null
                          && rp.Permission.PermissionName!= ""
                          && rp.Permission.IsActive)
             .Select(rp => rp.Permission!.PermissionName)
             .Distinct()
             .ToListAsync(cancellationToken);
        }

        var permissions = permissionList.Where(p =>!string.IsNullOrWhiteSpace(p)).ToHashSet();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenMinutes"]?? "15"));

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email?? string.Empty),
            new Claim("userId", user.Id.ToString()),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role)) continue;
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        foreach (var permission in permissions)
        {
            if (string.IsNullOrWhiteSpace(permission)) continue;
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

    public async Task<(RefreshToken entity, string rawToken)> GenerateRefreshTokenAsync(Guid userId, string ip, string userAgent, CancellationToken cancellationToken = default)
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
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"]?? "7")),
            Revoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (refreshToken, rawToken);
    }

    public async Task<(RefreshToken entity, string rawToken)> RotateRefreshTokenAsync(RefreshToken oldToken, string ip, string userAgent, CancellationToken cancellationToken = default)
    {
        if (oldToken.Revoked)
        {
            await RevokeAllUserTokensAsync(oldToken.UserId, ip, cancellationToken: cancellationToken);
            throw new SecurityTokenException("Token reuse detected - possible theft. All user sessions revoked.");
        }
        oldToken.Revoked = true;
        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.RevokedByIp = ip;
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
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"]?? "7")),
            Revoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (newToken, rawNewToken);
    }

    public string HashToken(string token)
    {
        var pepper = _config["Jwt:RefreshTokenPepper"]?? _config["Jwt:Key"]?? string.Empty;
        if (string.IsNullOrEmpty(pepper)) throw new InvalidOperationException("JWT pepper/key not configured");
        var pepperBytes = Encoding.UTF8.GetBytes(pepper);
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        using var hmac = new System.Security.Cryptography.HMACSHA256(pepperBytes);
        return Convert.ToBase64String(hmac.ComputeHash(tokenBytes));
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null, string? revokedBy = null, CancellationToken cancellationToken = default)
    {
        await _db.RefreshTokens.Where(t => t.UserId == userId &&!t.Revoked)
         .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true).SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedByIp, revokedByIp), cancellationToken);
        var auditLog = new AuditLog { Id = Guid.NewGuid(), UserId = userId, Action = "REVOKE_ALL_TOKENS", Entity = "RefreshToken", IpAddress = revokedByIp, CreatedBy = Guid.TryParse(revokedBy, out var parsedId)? parsedId : userId, Timestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RevokeTokenAsync(string rawToken, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash &&!t.Revoked, cancellationToken);
        if (token == null) return false;
        token.Revoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = revokedByIp;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool IsValid, RefreshToken? Token)> ValidateRefreshTokenAsync(string rawToken, Guid userId, string? currentIp = null, string? currentUserAgent = null, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId &&!t.Revoked && t.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (token == null) return (false, null);
        if (!IsSubnetMatch(token.IpAddress, currentIp)) await LogSecurityWarningAsync(userId, "IP_SUBNET_MISMATCH", token.IpAddress, currentIp, cancellationToken);
        return (true, token);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawToken);
        return await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash &&!t.Revoked && t.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetUserActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default) => await _db.RefreshTokens.Where(t => t.UserId == userId &&!t.Revoked && t.ExpiresAt > DateTime.UtcNow).OrderByDescending(t => t.CreatedAt).ToListAsync(cancellationToken);
    public async Task<int> GetUserActiveTokenCountAsync(Guid userId, CancellationToken cancellationToken = default) => await _db.RefreshTokens.CountAsync(t => t.UserId == userId &&!t.Revoked && t.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public async Task<(List<RefreshToken> Tokens, int Total)> GetUserTokensPaginatedAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _db.RefreshTokens.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var tokens = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (tokens, total);
    }

    public async Task<int> CleanExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var expiredTokens = await _db.RefreshTokens.Where(t => t.ExpiresAt < DateTime.UtcNow || (t.Revoked && t.RevokedAt < cutoffDate)).ToListAsync(cancellationToken);
        if (expiredTokens.Any()) { _db.RefreshTokens.RemoveRange(expiredTokens); await _db.SaveChangesAsync(cancellationToken); }
        return expiredTokens.Count;
    }

    private bool IsSubnetMatch(string? originalIp, string? currentIp)
    {
        if (string.IsNullOrEmpty(originalIp) || string.IsNullOrEmpty(currentIp)) return true;
        var a = originalIp.Split('.'); var b = currentIp.Split('.');
        if (a.Length == 4 && b.Length == 4) return a[0] == b[0] && a[1] == b[1] && a[2] == b[2];
        return originalIp == currentIp;
    }

    private async Task LogSecurityWarningAsync(Guid userId, string action, string? oldVal, string? newVal, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog { Id = Guid.NewGuid(), UserId = userId, Action = action, Entity = "RefreshToken", OldValues = oldVal, NewValues = newVal, Timestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private string GenerateRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}