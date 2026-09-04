using System.Security.Cryptography;
using University.Core.Interfaces.Services;

namespace University.Infrastructure.Services;

public class PasswordPolicyService : IPasswordPolicyService
{
    private const int MinLength = 12;

    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%^&*";

    public string GenerateTempPassword()
    {
        var upper = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        var lower = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        var digit = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        var symbol = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];

        var all = Upper + Lower + Digits + Symbols;
        var buffer = new char[MinLength];
        for (var i = 0; i < MinLength; i++)
        {
            buffer[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        // Guarantee one of each policy class present.
        buffer[0] = upper;
        buffer[1] = lower;
        buffer[2] = digit;
        buffer[3] = symbol;

        // Fisher-Yates shuffle so the required characters are not always in the leading positions.
        for (var i = MinLength - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }

        return new string(buffer);
    }

    public (bool Valid, string Reason) ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            return (false, $"Password must be at least {MinLength} characters.");
        }
        if (!password.Any(char.IsUpper))
        {
            return (false, "Password must contain at least one uppercase letter.");
        }
        if (!password.Any(char.IsLower))
        {
            return (false, "Password must contain at least one lowercase letter.");
        }
        if (!password.Any(char.IsDigit))
        {
            return (false, "Password must contain at least one number.");
        }
        if (!password.Any(IsSymbol))
        {
            return (false, "Password must contain at least one symbol (e.g. !@#$%^&*).");
        }
        return (true, string.Empty);
    }

    private static bool IsSymbol(char c) => Symbols.Contains(c) || char.IsPunctuation(c) || char.IsSymbol(c);
}