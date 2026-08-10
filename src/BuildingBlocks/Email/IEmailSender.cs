namespace Illumin360.Email;

/// <summary>Sends transactional emails over SMTP.</summary>
public interface IEmailSender
{
    /// <summary>Sends an HTML email.</summary>
    /// <param name="toAddress">Recipient address.</param>
    /// <param name="subject">Subject line.</param>
    /// <param name="htmlBody">HTML body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken);
}
