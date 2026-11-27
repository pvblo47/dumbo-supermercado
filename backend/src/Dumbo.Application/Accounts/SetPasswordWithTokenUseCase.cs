using Dumbo.Application.Abstractions;
using Dumbo.Domain.Enums;
using Dumbo.Domain.Rules;

namespace Dumbo.Application.Accounts;

/// <summary>
/// Command to set or reset a password using a single-use link token.
/// <see cref="Type"/> distinguishes between account activation and password recovery.
/// </summary>
public sealed record SetPasswordCommand(ActivationTokenType Type, string? Token, string? Password);

/// <summary>
/// Represents the outcome of attempting to set a password via token.
/// </summary>
public enum SetPasswordOutcome
{
    /// <summary>
    /// The password was updated and the account is active.
    /// </summary>
    Success,

    /// <summary>
    /// The provided password did not satisfy account complexity rules.
    /// </summary>
    InvalidPassword,

    /// <summary>
    /// The token was missing, unknown, expired, already used, or ineligible for the account state.
    /// </summary>
    InvalidToken
}

/// <summary>
/// Result returned by <see cref="SetPasswordWithTokenUseCase"/>, including optional validation errors.
/// </summary>
public sealed record SetPasswordResult(
    SetPasswordOutcome Outcome, IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>
/// Use case for completing account activation or password reset using a token link.
/// Enforces D06 rules: single-use tokens, 30-minute expiration, and no reactivation of Disabled or Deleted accounts.
/// </summary>
public sealed class SetPasswordWithTokenUseCase(
    IActivationTokenStore tokens, IAccountPasswordHasher hasher, TimeProvider clock)
{
    /// <summary>
    /// Custom message, shared across missing, expired, or already-used tokens.
    /// </summary>
    public const string InvalidTokenMessage = "El enlace no es válido o ya venció";

    /// <summary>
    /// Validates the password and token, verifies account eligibility, and atomically redeems the token.
    /// </summary>
    public async Task<SetPasswordResult> ExecuteAsync(SetPasswordCommand command, CancellationToken cancellationToken)
    {
        // Validate password BEFORE touching the token so a weak password leaves the token usable.
        if (!AccountRules.IsValidPassword(command.Password))
            return new SetPasswordResult(
                SetPasswordOutcome.InvalidPassword,
                new Dictionary<string, string[]> { ["password"] = [AccountRules.WeakPasswordMessage] });

        if (string.IsNullOrWhiteSpace(command.Token))
            return Invalid();

        var now = clock.GetUtcNow().UtcDateTime;
        var token = await tokens.FindByHashAsync(SecureToken.Hash(command.Token.Trim()), cancellationToken);

        if (token is null || token.Type != command.Type || !token.IsValidAt(now))
            return Invalid();

        // Activation applies only to PendingActivation accounts; password reset applies only to Active accounts.
        // Disabled or Deleted accounts are rejected so no link ever reactivates them.
        var eligible = command.Type switch
        {
            ActivationTokenType.AccountActivation => token.User.Status == UserStatus.PendingActivation,
            ActivationTokenType.PasswordReset => token.User.Status == UserStatus.Active,
            _ => false
        };
        if (!eligible)
            return Invalid();

        // Atomic redemption prevents two concurrent requests from consuming the same token.
        var redeemed = await tokens.RedeemAsync(
            token.Id, token.UserId, hasher.Hash(command.Password!), token.User.Status, token.User.SessionVersion,
            clock.GetUtcNow().UtcDateTime, cancellationToken);

        return redeemed ? new SetPasswordResult(SetPasswordOutcome.Success) : Invalid();
    }

    private static SetPasswordResult Invalid() => new(SetPasswordOutcome.InvalidToken);
}
