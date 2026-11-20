namespace Dumbo.Api.Contracts;

/// <summary>
/// Request payload for <c>POST /api/auth/password/forgot</c>.
/// </summary>
/// <param name="Email">The account email address to evaluate for password recovery or activation resend.</param>
public sealed record ForgotPasswordRequest(string? Email);

/// <summary>
/// Request payload for <c>POST /api/auth/activate</c> and <c>POST /api/auth/password/reset</c>.
/// </summary>
/// <param name="Token">Single-use token value extracted from the <c>token</c> query parameter of the email link.</param>
/// <param name="Password">New password chosen by the user.</param>
public sealed record SetPasswordRequest(string? Token, string? Password);