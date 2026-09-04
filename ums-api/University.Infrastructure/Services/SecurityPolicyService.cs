using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using University.Core.Interfaces.Services.Security;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.Configuration;

namespace University.Infrastructure.Services;

public class SecurityPolicyService : ISecurityPolicyService
{
    private readonly ApplicationDbContext _context;
    private readonly SecuritySettings _defaults;

    public SecurityPolicyService(ApplicationDbContext context, IOptions<SecuritySettings> options)
    {
        _context = context;
        _defaults = options.Value;
    }

    public async Task<Result<SecuritySettings>> GetAsync()
    {
        // Deep copy defaults so we never mutate the singleton options instance.
        var settings = Clone(_defaults);

        var overrides = await _context.SecuritySettingsOverrides.ToListAsync();
        foreach (var row in overrides)
        {
            ApplyOverride(settings, row.Key, row.ValueJson);
        }

        return Result<SecuritySettings>.Success(settings);
    }

    public async Task<Result<bool>> UpdateAsync(SecuritySettings settings)
    {
        // Persist primitive overrides to the security_settings_overrides table so changes survive restarts.
        var existing = await _context.SecuritySettingsOverrides.ToListAsync();
        _context.SecuritySettingsOverrides.RemoveRange(existing);

        AddOverride("PasswordPolicy:MinLength", settings.PasswordPolicy.MinLength.ToString());
        AddOverride("PasswordPolicy:RequireUppercase", settings.PasswordPolicy.RequireUppercase.ToString());
        AddOverride("PasswordPolicy:RequireLowercase", settings.PasswordPolicy.RequireLowercase.ToString());
        AddOverride("PasswordPolicy:RequireDigit", settings.PasswordPolicy.RequireDigit.ToString());
        AddOverride("PasswordPolicy:RequireSpecial", settings.PasswordPolicy.RequireSpecial.ToString());
        AddOverride("PasswordPolicy:ExpirationDays", settings.PasswordPolicy.ExpirationDays.ToString());
        AddOverride("PasswordPolicy:HistoryCount", settings.PasswordPolicy.HistoryCount.ToString());
        AddOverride("PasswordPolicy:MaxFailedAttempts", settings.PasswordPolicy.MaxFailedAttempts.ToString());
        AddOverride("PasswordPolicy:LockoutMinutes", settings.PasswordPolicy.LockoutMinutes.ToString());
        AddOverride("Jwt:AccessTokenMinutes", settings.Jwt.AccessTokenMinutes.ToString());
        AddOverride("Jwt:RefreshTokenDays", settings.Jwt.RefreshTokenDays.ToString());
        AddOverride("TwoFactor:Enabled", settings.TwoFactor.Enabled.ToString());
        AddOverride("TwoFactor:RecoveryCodesCount", settings.TwoFactor.RecoveryCodesCount.ToString());

        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private void AddOverride(string key, string value)
    {
        _context.SecuritySettingsOverrides.Add(new University.Core.Entities.SecuritySettingsOverride
        {
            Key = key,
            ValueJson = JsonSerializer.Serialize(value)
        });
    }

    private static void ApplyOverride(SecuritySettings settings, string key, string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson))
        {
            return;
        }

        string value;
        try
        {
            value = JsonSerializer.Deserialize<string>(valueJson) ?? string.Empty;
        }
        catch
        {
            value = valueJson.Trim('"');
        }

        switch (key)
        {
            case "PasswordPolicy:MinLength": if (int.TryParse(value, out var a)) settings.PasswordPolicy.MinLength = a; break;
            case "PasswordPolicy:RequireUppercase": if (bool.TryParse(value, out var b)) settings.PasswordPolicy.RequireUppercase = b; break;
            case "PasswordPolicy:RequireLowercase": if (bool.TryParse(value, out var c)) settings.PasswordPolicy.RequireLowercase = c; break;
            case "PasswordPolicy:RequireDigit": if (bool.TryParse(value, out var d)) settings.PasswordPolicy.RequireDigit = d; break;
            case "PasswordPolicy:RequireSpecial": if (bool.TryParse(value, out var e)) settings.PasswordPolicy.RequireSpecial = e; break;
            case "PasswordPolicy:ExpirationDays": if (int.TryParse(value, out var f)) settings.PasswordPolicy.ExpirationDays = f; break;
            case "PasswordPolicy:HistoryCount": if (int.TryParse(value, out var g)) settings.PasswordPolicy.HistoryCount = g; break;
            case "PasswordPolicy:MaxFailedAttempts": if (int.TryParse(value, out var h)) settings.PasswordPolicy.MaxFailedAttempts = h; break;
            case "PasswordPolicy:LockoutMinutes": if (int.TryParse(value, out var i)) settings.PasswordPolicy.LockoutMinutes = i; break;
            case "Jwt:AccessTokenMinutes": if (int.TryParse(value, out var j)) settings.Jwt.AccessTokenMinutes = j; break;
            case "Jwt:RefreshTokenDays": if (int.TryParse(value, out var k)) settings.Jwt.RefreshTokenDays = k; break;
            case "TwoFactor:Enabled": if (bool.TryParse(value, out var l)) settings.TwoFactor.Enabled = l; break;
            case "TwoFactor:RecoveryCodesCount": if (int.TryParse(value, out var m)) settings.TwoFactor.RecoveryCodesCount = m; break;
        }
    }

    private static SecuritySettings Clone(SecuritySettings source) =>
        JsonSerializer.Deserialize<SecuritySettings>(
            JsonSerializer.Serialize(source, new JsonSerializerOptions
            {
                WriteIndented = false,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            })) ?? new SecuritySettings();
}
