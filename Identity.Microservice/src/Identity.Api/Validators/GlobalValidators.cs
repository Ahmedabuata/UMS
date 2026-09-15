using System.Text.RegularExpressions;

namespace Identity.Api.Validators;

/// <summary>
/// GlobalValidators - نسخة مطابقة 100% لقيود DB identity_db
/// 
/// NOTE: Validation logic is progressively moved to FluentValidation.
/// However, legacy methods (IsValidEmail, ValidatePassword, ...) are kept
/// for backward compatibility with AuthController, UserService, and
/// existing code paths. New code should use FluentValidation validators.
/// </summary>
public static class GlobalValidators
{
    // ============================================================
    // CONSTANTS (used by both FluentValidation and legacy code)
    // ============================================================

    // DB: chk_users_email
    public const string EmailPattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
    private static readonly Regex EmailRegex = new(EmailPattern, RegexOptions.Compiled);

    // DB: chk_users_username (length >= 3)
    public const string UsernamePattern = @"^[a-zA-Z0-9_.-]+$";
    private static readonly Regex UsernameRegex = new(UsernamePattern, RegexOptions.Compiled);
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 50;

    // DB: chk_users_phone (7-15 digits, nullable)
    public const string PhonePattern = @"^(\+|00)?[0-9()./ -]*[0-9]$";
    private static readonly Regex PhoneRegex = new(PhonePattern, RegexOptions.Compiled);
    public const int PhoneMinDigits = 7;
    public const int PhoneMaxDigits = 15;

    // DB: chk_roles_rolename
    public static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "USER", "STUDENT", "FACULTY"
    };

    // Password policy
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    // ============================================================
    // LEGACY METHODS (backward compatibility)
    // ============================================================

    public static bool IsValidEmail(string? email)
        => !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());

    public static bool IsValidUsername(string? username)
        => !string.IsNullOrWhiteSpace(username)
           && username.Trim().Length >= UsernameMinLength
           && UsernameRegex.IsMatch(username.Trim());

    public static bool IsValidRoleName(string? roleName)
        => !string.IsNullOrWhiteSpace(roleName) && AllowedRoles.Contains(roleName.Trim());

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true; // phone nullable

        var trimmed = phone.Trim();
        if (!PhoneRegex.IsMatch(trimmed)) return false;

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length >= PhoneMinDigits && digits.Length <= PhoneMaxDigits;
    }

    /// <summary>
    /// Validates password complexity (length, uppercase, lowercase, digit, special).
    /// Returns null if valid, otherwise the error message.
    /// </summary>
    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < PasswordMinLength)
            return $"Password must be at least {PasswordMinLength} characters long.";

        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";

        if (!password.Any(char.IsLower))
            return "Password must contain at least one lowercase letter.";

        if (!password.Any(char.IsDigit))
            return "Password must contain at least one numeric digit.";

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            return "Password must contain at least one special character.";

        return null;
    }

    /// <summary>
    /// Validates email + username + phone (and optional role).
    /// Returns null if valid, otherwise the error message.
    /// </summary>
    public static string? ValidateUserInput(string? email, string? username, string? phone, string? roleName = null)
    {
        if (!IsValidEmail(email)) return $"Invalid email format. Must match {EmailPattern}";
        if (!IsValidUsername(username)) return $"Invalid username. Min {UsernameMinLength} chars and must match {UsernamePattern}";
        if (!IsValidPhone(phone)) return $"Invalid phone. Must match {PhonePattern} with {PhoneMinDigits}-{PhoneMaxDigits} digits.";
        if (roleName != null && !IsValidRoleName(roleName)) return $"Invalid role. Allowed: {string.Join(",", AllowedRoles)}";
        return null;
    }

    // ============================================================
    // NORMALIZATION HELPERS (null-safe - supports nullable inputs)
    // ============================================================

    /// <summary>
    /// Normalizes email (trim + lowercase). Returns empty string on null.
    /// Accepts nullable input to support Controllers passing request fields.
    /// </summary>
    public static string NormalizeEmail(string? email)
        => email?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>
    /// Normalizes username (trim). Returns empty string on null.
    /// Accepts nullable input to support Controllers passing request fields.
    /// </summary>
    public static string NormalizeUsername(string? username)
        => username?.Trim() ?? string.Empty;
}