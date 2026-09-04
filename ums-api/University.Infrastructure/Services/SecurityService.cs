using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using University.Core.Interfaces.Services.Security;
using University.Shared.Common;

namespace University.Infrastructure.Services;

public class SecurityService : ISecurityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<Result<Guid?>> GetCurrentUserIdAsync()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirstValue("userId") ?? _httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return Task.FromResult(Result<Guid?>.Success(userId));
        }

        return Task.FromResult(Result<Guid?>.Unauthorized("NO_USER_CONTEXT", "No authenticated user context available."));
    }

    public Task<Result<bool>> IsInRoleAsync(string role)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null)
        {
            return Task.FromResult(Result<bool>.Unauthorized("NO_USER_CONTEXT", "No authenticated user context available."));
        }

        var isInRole = principal.IsInRole(role)
            || principal.FindFirstValue("role")?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;

        return Task.FromResult(Result<bool>.Success(isInRole));
    }
}
