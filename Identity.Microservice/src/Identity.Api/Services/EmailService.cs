using System.Net;
using System.Net.Mail;
using Identity.Api.Configurations;
using Identity.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace Identity.Api.Services;

/// <summary>
/// SMTP-based email sender.
/// Works with Gmail (production) and Mailtrap (development).
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string username,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass),
                EnableSsl = _settings.EnableSsl,
            };

            var subject = "Welcome to UMS - Your Account Details";

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; direction: ltr; max-width: 600px; margin: 0 auto;'>
    <div style='background: #f8f9fa; padding: 20px; border-radius: 8px;'>
        <h2 style='color: #2c3e50;'>Welcome to Identity Management System</h2>
        <p>Hello <strong>{username}</strong>,</p>
        <p>Your account has been created successfully. Below are your login credentials:</p>
        
        <table style='border-collapse: collapse; margin: 20px 0; width: 100%;'>
            <tr>
                <td style='padding: 12px; border: 1px solid #ddd; background: #fff; width: 40%;'>
                    <strong>Username:</strong>
                </td>
                <td style='padding: 12px; border: 1px solid #ddd; background: #fff;'>
                    {username}
                </td>
            </tr>
            <tr>
                <td style='padding: 12px; border: 1px solid #ddd; background: #fff;'>
                    <strong>Temporary Password:</strong>
                </td>
                <td style='padding: 12px; border: 1px solid #ddd; background: #fff; font-family: monospace; font-size: 16px; color: #d9534f;'>
                    {temporaryPassword}
                </td>
            </tr>
        </table>
        
        <div style='background: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin: 20px 0;'>
            <strong>⚠️ Important:</strong> You will be required to change your password on first login.
        </div>
        
        <p style='color: #6c757d; font-size: 14px;'>
            For security reasons, please do not share this password with anyone.
        </p>
        
        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 20px 0;' />
        <p style='color: #6c757d; font-size: 12px;'>
            Best regards,<br/>UMS Team
        </p>
    </div>
</body>
</html>";

            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Welcome email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            throw;
        }
    }
}