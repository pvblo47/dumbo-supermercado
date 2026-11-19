namespace Dumbo.Application.Email;

/// <summary>
/// Thrown when an email provider fails to deliver an outbound message.
/// The exception message is safe to persist in storage: senders never include email bodies, links, or tokens in it.
/// </summary>
public sealed class EmailDeliveryException(string message) : Exception(message);