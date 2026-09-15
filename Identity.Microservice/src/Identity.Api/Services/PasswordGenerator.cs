using System.Security.Cryptography;
using Identity.Api.Interfaces;

namespace Identity.Api.Services;

/// <summary>
/// Cryptographically secure password generator.
/// Uses RandomNumberGenerator (NOT System.Random) for security.
/// Excludes ambiguous characters: 0/O, 1/l/I.
/// </summary>
public class PasswordGenerator : IPasswordGenerator
{
    // Avoid ambiguous characters that are hard to read in emails
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%^&*";

    public string Generate(int length = 12)
    {
        // Enforce minimum length
        if (length < 8) length = 8;

        var allChars = Uppercase + Lowercase + Digits + Special;
        var password = new char[length];

        // Guarantee at least one of each required type
        password[0] = Uppercase[RandomNumberGenerator.GetInt32(Uppercase.Length)];
        password[1] = Lowercase[RandomNumberGenerator.GetInt32(Lowercase.Length)];
        password[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        password[3] = Special[RandomNumberGenerator.GetInt32(Special.Length)];

        // Fill remaining with random chars
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        // Fisher-Yates shuffle to avoid predictable positions
        for (int i = length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}