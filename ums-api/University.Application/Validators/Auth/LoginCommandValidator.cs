using University.Shared.DTOs.Auth;

namespace University.Application.Validators.Auth;

public class LoginCommandValidator
{
    public IDictionary<string, string[]> Validate(LoginRequestDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            errors["Email"] = new[] { "Email is required." };
        }
        else if (!dto.Email.Contains('@'))
        {
            errors["Email"] = new[] { "Email is not a valid email address." };
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            errors["Password"] = new[] { "Password is required." };
        }

        return errors;
    }
}
