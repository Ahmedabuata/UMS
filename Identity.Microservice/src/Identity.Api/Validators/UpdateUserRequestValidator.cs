using FluentValidation;
using Identity.Api.DTOs;

namespace Identity.Api.Validators;

/// <summary>
/// Validator for UpdateUserRequest.
/// All fields optional (PATCH semantics via PUT).
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(GlobalValidators.UsernameMinLength)
                .WithMessage($"Username must be at least {GlobalValidators.UsernameMinLength} characters.")
            .MaximumLength(GlobalValidators.UsernameMaxLength)
                .WithMessage($"Username must not exceed {GlobalValidators.UsernameMaxLength} characters.")
            .Matches(GlobalValidators.UsernamePattern)
                .WithMessage("Username can only contain letters, digits, dots, underscores, and hyphens.")
            .When(x => !string.IsNullOrWhiteSpace(x.Username));

        RuleFor(x => x.Email)
            .Matches(GlobalValidators.EmailPattern)
                .WithMessage("Email format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(GlobalValidators.PhonePattern)
                .WithMessage("Phone format is invalid.")
            .Must(BeValidPhoneDigits)
                .WithMessage($"Phone must contain {GlobalValidators.PhoneMinDigits}-{GlobalValidators.PhoneMaxDigits} digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }

    private static bool BeValidPhoneDigits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length >= GlobalValidators.PhoneMinDigits
            && digits.Length <= GlobalValidators.PhoneMaxDigits;
    }
}