using Microsoft.Extensions.Options;
using University.Shared.Configuration;

namespace University.Application.Configuration;

public class SecuritySettingsValidator : IValidateOptions<SecuritySettings>
{
    public ValidateOptionsResult Validate(string? name, SecuritySettings options)
    {
        var failures = new List<string>();

        if (options.PasswordPolicy.MinLength < 6)
        {
            failures.Add("SecuritySettings:PasswordPolicy:MinLength must be at least 6.");
        }
        if (options.Jwt.AccessTokenMinutes <= 0)
        {
            failures.Add("SecuritySettings:Jwt:AccessTokenMinutes must be greater than 0.");
        }
        if (options.Jwt.RefreshTokenDays <= 0)
        {
            failures.Add("SecuritySettings:Jwt:RefreshTokenDays must be greater than 0.");
        }
        if (string.IsNullOrWhiteSpace(options.Jwt.SecretKey))
        {
            failures.Add("SecuritySettings:Jwt:SecretKey must be provided (via environment or appsettings).");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
