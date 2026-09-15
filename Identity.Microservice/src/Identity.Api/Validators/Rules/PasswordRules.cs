using FluentValidation;

namespace Identity.Api.Validators.Rules;

/// <summary>
/// Reusable password validation rules.
/// Aligned with GlobalValidators + DB constraints.
/// Usage: RuleFor(x => x.Password).StrongPassword();
/// </summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string?> StrongPassword<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MinimumLength(GlobalValidators.PasswordMinLength)
                .WithMessage($"Password must be at least {GlobalValidators.PasswordMinLength} characters.")
            .MaximumLength(GlobalValidators.PasswordMaxLength)
                .WithMessage($"Password must not exceed {GlobalValidators.PasswordMaxLength} characters.")
            .Matches(@"[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]")
                .WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]")
                .WithMessage("Password must contain at least one numeric digit.")
            .Matches(@"[^a-zA-Z0-9]")
                .WithMessage("Password must contain at least one special character.");
    }
}