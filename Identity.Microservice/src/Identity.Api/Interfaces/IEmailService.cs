namespace Identity.Api.Interfaces;

/// <summary>
/// Sends transactional emails (welcome, password reset, etc.).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a welcome email containing the temporary password.
    /// The user must change this password on first login.
    /// </summary>
    Task SendWelcomeEmailAsync(
        string toEmail,
        string username,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}