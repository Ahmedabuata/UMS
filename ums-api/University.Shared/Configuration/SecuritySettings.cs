namespace University.Shared.Configuration;

/// <summary>
/// Typed security configuration bound from the "SecuritySettings" section of appsettings.json.
/// Lives in Shared so the Core service layer can reference it without an Application dependency.
/// Runtime overrides are persisted via the security_settings_overrides table so updates survive
/// restarts without rewriting the on-disk config file.
/// </summary>
public class SecuritySettings
{
    public PasswordPolicyConfig PasswordPolicy { get; set; } = new();
    public JwtConfig Jwt { get; set; } = new();
    public TwoFactorConfig TwoFactor { get; set; } = new();

    public class PasswordPolicyConfig
    {
        public int MinLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecial { get; set; } = true;
        public int ExpirationDays { get; set; } = 90;
        public int HistoryCount { get; set; } = 5;
        public int MaxFailedAttempts { get; set; } = 5;
        public int LockoutMinutes { get; set; } = 30;
    }

    public class JwtConfig
    {
        public string SecretKey { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 7;
        public string Issuer { get; set; } = "UMS";
        public string Audience { get; set; } = "UMS";
    }

    public class TwoFactorConfig
    {
        public bool Enabled { get; set; } = false;
        public string Issuer { get; set; } = "UMS";
        public string QrCodeProvider { get; set; } = "QRCoder";
        public int RecoveryCodesCount { get; set; } = 8;
    }
}
