using University.Shared.DTOs.Auth;

namespace University.Application.Validators.Auth;

public class LoginCommandValidator
{
    public IDictionary<string, string[]> Validate(LoginRequestDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            errors["Username"] = new[] { "Username is required." };
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            errors["Password"] = new[] { "Password is required." };
        }

        return errors;
    }
}
