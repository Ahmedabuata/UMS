using FluentValidation;
using Identity.Api.DTOs;
using Identity.Api.Validators.Rules;

namespace Identity.Api.Validators;

/// <summary>
/// Validator for PatchUserPasswordRequest.
/// Requires: current password + strong new password.
/// </summary>
public class PatchUserPasswordRequestValidator : AbstractValidator<PatchUserPasswordRequest>
{
    public PatchUserPasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .StrongPassword();

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
                .WithMessage("New password must be different from current password.");
    }
}