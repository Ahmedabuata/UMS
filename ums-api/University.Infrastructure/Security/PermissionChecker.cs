namespace University.Infrastructure.Security;

public interface IPermissionChecker
{
    bool HasPermission(IEnumerable<string> userPermissions, string requiredPermission) =>
        userPermissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase);
}

public class PermissionChecker : IPermissionChecker
{
    public bool HasPermission(IEnumerable<string> userPermissions, string requiredPermission) =>
        userPermissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase);
}
