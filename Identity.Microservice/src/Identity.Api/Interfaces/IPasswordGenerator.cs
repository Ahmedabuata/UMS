namespace Identity.Api.Interfaces;

/// <summary>
/// Generates cryptographically secure random passwords.
/// Used when SendPasswordByEmail = true.
/// </summary>
public interface IPasswordGenerator
{
    /// <summary>
    /// Generates a random password with:
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// </summary>
    /// <param name="length">Password length (min 8, default 12)</param>
    string Generate(int length = 12);
}