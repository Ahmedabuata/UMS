using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace University.Infrastructure.Security;

/// <summary>
/// Dynamic authorization policy provider.
///
/// Resolves policy names of the form "HasPermission:&lt;PERMISSION&gt;" into an
/// AuthorizationPolicy carrying a PermissionRequirement with the specified
/// permission name, and "SuperAdminOnly" into a SuperAdminRequirement.
///
/// This avoids calling options.AddPolicy(...) for every individual permission;
/// the provider parses the policy name on demand. Explicitly-configured policies
/// (e.g. "HasPermission") are still served by the base provider.
/// </summary>
public class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider, IAuthorizationPolicyProvider
{
    private const string PermissionPolicyPrefix = "HasPermission:";
    private const string SuperAdminOnly = "SuperAdminOnly";

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Serve any explicitly configured policy first (e.g. "HasPermission").
        var namedPolicy = await base.GetPolicyAsync(policyName);
        if (namedPolicy != null)
        {
            return namedPolicy;
        }

        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName.Substring(PermissionPolicyPrefix.Length);
            var builder = new AuthorizationPolicyBuilder();
            builder.AddRequirements(new PermissionRequirement(string.IsNullOrWhiteSpace(permission) ? null : permission));
            return builder.Build();
        }

        if (string.Equals(policyName, SuperAdminOnly, StringComparison.Ordinal))
        {
            var builder = new AuthorizationPolicyBuilder();
            builder.AddRequirements(new SuperAdminRequirement());
            return builder.Build();
        }

        return null;
    }
}
