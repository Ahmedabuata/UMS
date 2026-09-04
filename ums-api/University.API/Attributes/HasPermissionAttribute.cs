using Microsoft.AspNetCore.Authorization;

namespace University.API.Attributes;

/// <summary>
/// Shorthand for [Authorize(Policy = "HasPermission:&lt;PERMISSION&gt;")].
/// The dynamic policy is resolved by PermissionAuthorizationPolicyProvider.
/// Example: [HasPermission("FINANCE_READ")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    private const string PolicyPrefix = "HasPermission:";

    public HasPermissionAttribute(string permission)
        : base(PolicyPrefix + permission)
    {
    }
}
