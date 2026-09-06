using System.Text.RegularExpressions;

namespace Identity.Api.Validators;

/// <summary>
/// GlobalValidators - نسخة مطابقة 100% لقيود DB identity_db
/// لا تعديل على regex - نفس chk_ الموجودة في DB
/// </summary>
public static class GlobalValidators
{
    // DB: chk_users_email ^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$
    public const string EmailPattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
    private static readonly Regex EmailRegex = new(EmailPattern, RegexOptions.Compiled);

    // DB: chk_users_username length>=3 ^[a-zA-Z0-9_.-]+$
    public const string UsernamePattern = @"^[a-zA-Z0-9_.-]+$";
    private static readonly Regex UsernameRegex = new(UsernamePattern, RegexOptions.Compiled);
    public const int UsernameMinLength = 3;

    // DB: chk_roles_rolename ENUM SUPER_ADMIN,ADMIN,HR_MANAGER,USER,STUDENT,FACULTY
    public static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        "SUPER_ADMIN", "ADMIN", "HR_MANAGER", "USER", "STUDENT", "FACULTY"
    };

    // DB: chk_users_phone العالمي ^(\+|00)?[0-9()./ -]*[0-9]$ 7-15 رقم
    public const string PhonePattern = @"^(\+|00)?[0-9()./ -]*[0-9]$";
    private static readonly Regex PhoneRegex = new(PhonePattern, RegexOptions.Compiled);

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
        if (string.IsNullOrWhiteSpace(phone)) return true; // phone nullable في DB

        var trimmed = phone.Trim();
        if (!PhoneRegex.IsMatch(trimmed)) return false;

        // عد الأرقام فقط 7-15
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length >= 7 && digits.Length <= 15;
    }

    /// <summary>
    /// التحقق من قواعد تعقيد كلمة السر (طول، أحرف كبيرة وصغيرة، أرقام، ورموز)
    /// </summary>
    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return "Password must be at least 8 characters long.";

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

    public static string? ValidateUserInput(string? email, string? username, string? phone, string? roleName = null)
    {
        if (!IsValidEmail(email)) return $"Invalid email format. Must match {EmailPattern}";
        if (!IsValidUsername(username)) return $"Invalid username. Min {UsernameMinLength} chars and must match {UsernamePattern}";
        if (!IsValidPhone(phone)) return $"Invalid phone. Must match {PhonePattern} with 7-15 digits. Tested valid: +32 (0)467 88 32 18";
        if (roleName != null && !IsValidRoleName(roleName)) return $"Invalid role. Allowed: {string.Join(",", AllowedRoles)}";
        return null;
    }

    // Helper لـ frontend mapping camelCase
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    public static string NormalizeUsername(string username) => username.Trim();
}