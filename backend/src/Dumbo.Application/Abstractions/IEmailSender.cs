using Dumbo.Application.Email;

namespace Dumbo.Application.Abstractions;

/// <summary>
/// Delivers an email message to an external mail provider (such as Resend).
/// The Application layer depends only on this abstraction.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends the specified email message asynchronously.
    /// </summary>
    /// <exception cref="EmailDeliveryException">Thrown when the provider does not accept or receive the message.</exception>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}