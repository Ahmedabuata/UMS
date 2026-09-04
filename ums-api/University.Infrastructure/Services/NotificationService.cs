using Microsoft.Extensions.Logging;

namespace University.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void SendWelcomeEmail(string toEmail, string username, string tempPassword)
    {
        // Mock notification (nodemailer equivalent). Log to console until an SMTP client is wired up.
        _logger.LogInformation(
            "[MOCK EMAIL] To: {Email} | Subject: Your UMS account is ready | Body: Your temporary password is {Password} (username: {Username}). You must change it on first login.",
            toEmail, tempPassword, username);
    }

    public void SendWelcomeSms(string phoneNumber, string username, string tempPassword)
    {
        // Mock notification (twilio equivalent). Log to console until a Twilio client is wired up.
        _logger.LogInformation(
            "[MOCK SMS] To: {Phone} | Your UMS temporary password is {Password} (username: {Username}). Change it on first login.",
            phoneNumber, tempPassword, username);
    }
}