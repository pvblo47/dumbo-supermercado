namespace Dumbo.Application.Email;

/// <summary>
/// Represents an outbound email message ready for delivery, containing both HTML and plain-text bodies.
/// </summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);