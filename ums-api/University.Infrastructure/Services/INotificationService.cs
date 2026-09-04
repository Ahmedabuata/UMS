using System.Security.Cryptography;

namespace University.Infrastructure.Services;

public interface INotificationService
{
    void SendWelcomeEmail(string toEmail, string username, string tempPassword);
    void SendWelcomeSms(string phoneNumber, string username, string tempPassword);
}