namespace Dumbo.Application.Email;

/// <summary>
/// Public base URL of the Angular web application used to construct links inside outbound emails.
/// Configured via "App:PublicBaseUrl" (defaults to http://localhost:8080).
/// </summary>
public sealed record EmailLinkSettings(string PublicBaseUrl);