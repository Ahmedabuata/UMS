using FluentValidation;
using Identity.Api.DTOs;
using Identity.Api.Validators.Rules;

namespace Identity.Api.Validators;

/// <summary>
/// Validator for CreateUserRequestDto.
/// Supports: SendPasswordByEmail flag (bypasses Password validation).
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestValidator()
    {
        // ============================================================
        // USERNAME
        // ============================================================
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(GlobalValidators.UsernameMinLength)
                .WithMessage($"Username must be at least {GlobalValidators.UsernameMinLength} characters.")
            .MaximumLength(GlobalValidators.UsernameMaxLength)
                .WithMessage($"Username must not exceed {GlobalValidators.UsernameMaxLength} characters.")
            .Matches(GlobalValidators.UsernamePattern)
                .WithMessage("Username can only contain letters, digits, dots, underscores, and hyphens.");

        // ============================================================
        // EMAIL
        // ============================================================
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(GlobalValidators.EmailPattern)
                .WithMessage("Email format is invalid.");

        // ============================================================
        // PHONE (optional)
        // ============================================================
        RuleFor(x => x.PhoneNumber)
            .Matches(GlobalValidators.PhonePattern)
                .WithMessage("Phone format is invalid.")
            .Must(BeValidPhoneDigits)
                .WithMessage($"Phone must contain {GlobalValidators.PhoneMinDigits}-{GlobalValidators.PhoneMaxDigits} digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        // ============================================================
        // ROLE (optional)
        // ============================================================
        RuleFor(x => x.RoleName)
            .Must(role => GlobalValidators.AllowedRoles.Contains(role!))
                .WithMessage($"Invalid role. Allowed: {string.Join(", ", GlobalValidators.AllowedRoles)}")
            .When(x => !string.IsNullOrWhiteSpace(x.RoleName));

        // ============================================================
        // PASSWORD - required only if NOT sending by email
        // ============================================================
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .StrongPassword()
            .When(x => !x.SendPasswordByEmail);

        // ============================================================
        // PASSWORD must be empty when SendPasswordByEmail = true
        // ============================================================
        RuleFor(x => x.Password)
            .Empty().WithMessage("Password must not be provided when SendPasswordByEmail is true.")
            .When(x => x.SendPasswordByEmail);
    }

    private static bool BeValidPhoneDigits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length >= GlobalValidators.PhoneMinDigits
            && digits.Length <= GlobalValidators.PhoneMaxDigits;
    }
}