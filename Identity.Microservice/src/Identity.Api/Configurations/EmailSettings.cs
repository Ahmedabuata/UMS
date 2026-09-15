namespace Identity.Api.Configurations;

/// <summary>
/// SMTP configuration - bound from appsettings.json "EmailSettings" section.
/// </summary>
public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPass { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "UMS System";
    public bool EnableSsl { get; set; } = true;
}