namespace Dumbo.Domain.Enums;

/// <summary>
/// Specifies the purpose of a token link sent via email.
/// Matches the shared SQL schema values (1 = account activation, 2 = password reset)
/// and identifies the message type in the email queue.
/// </summary>
public enum ActivationTokenType
{
    /// <summary>
    /// Token used to activate a newly registered user account.
    /// </summary>
    AccountActivation = 1,

    /// <summary>
    /// Token used to reset a forgotten account password.
    /// </summary>
    PasswordReset = 2
}