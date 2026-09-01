using Microsoft.Extensions.Logging;
using University.Shared.Common;
using University.Shared.Exceptions;

namespace University.Application.Behaviors;

public class AuthorizationBehavior
{
    private readonly ILogger _logger;

    public AuthorizationBehavior(ILogger logger)
    {
        _logger = logger;
    }

    public void EnsurePermission(IEnumerable<string> userPermissions, string requiredPermission)
    {
        if (!userPermissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("User is missing required permission: {Permission}", requiredPermission);
            throw new ForbiddenException($"Missing required permission: {requiredPermission}");
        }
    }
}
