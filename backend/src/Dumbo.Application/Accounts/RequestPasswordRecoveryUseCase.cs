using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Dumbo.Domain.Rules;

namespace Dumbo.Application.Accounts;

/// <summary>
/// Use case for password recovery and activation link resending
/// Only queues an outbox email so background workers handle delivery, and intentionally returns no result
/// so the API can give a uniform response whether or not the account exists.
/// </summary>
public sealed class RequestPasswordRecoveryUseCase(
    IAccountLookup lookup, IEmailOutboxStore outbox, TimeProvider clock)
{
    /// <summary>
    /// Validates eligibility for the given email address and queues a password reset or activation email if applicable.
    /// </summary>
    public async Task ExecuteAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var user = await lookup.FindByEmailAsync(AccountRules.NormalizeEmail(email), cancellationToken);
        if (user is null)
            return;

        // Active -> password reset link. PendingActivation -> resend activation link
        // (RF-002: expired or previous links are replaced via a fresh token to the same address).
        // Disabled or Deleted -> no-op: recovery does not reactivate those states (D06, RF-001).
        ActivationTokenType? kind = user.Status switch
        {
            UserStatus.Active => ActivationTokenType.PasswordReset,
            UserStatus.PendingActivation => ActivationTokenType.AccountActivation,
            _ => null
        };
        if (kind is null)
            return;

        // Skip if a matching email is already pending to prevent inbox flooding from repeated requests.
        if (await outbox.HasPendingAsync(user.Id, kind.Value, cancellationToken))
            return;

        await outbox.EnqueueAsync(
            EmailOutboxItem.Create(user.Id, user.Email, kind.Value, clock.GetUtcNow().UtcDateTime),
            cancellationToken);
    }
}