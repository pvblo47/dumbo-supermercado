using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Clients;

/// <summary>
/// Enqueues a new activation link email for a client account still in <see cref="UserStatus.PendingActivation"/>
/// Previous tokens are invalidated by <c>IActivationTokenStore.ReplaceAsync</c> when the worker dispatches the message.
/// </summary>
public sealed class ResendActivationLinkUseCase(
    IAccountLookup lookup, IEmailOutboxStore outbox, TimeProvider clock)
{
    /// <summary>
    /// Validates client eligibility and idempotently queues an account activation email.
    /// </summary>
    public async Task<ResendActivationOutcome> ExecuteAsync(Guid clientId, CancellationToken cancellationToken, UserRole role = UserRole.Client)
    {
        var user = await lookup.FindByIdAsync(clientId, cancellationToken);

        // Out of scope: non-existent, deleted, or non-client accounts
        if (user is null || user.Role != role || user.Status == UserStatus.Deleted)
            return ResendActivationOutcome.NotFound;

        // Activation links cannot reactivate disabled or already-active accounts (D06).
        if (user.Status != UserStatus.PendingActivation)
            return ResendActivationOutcome.NotPending;

        // Idempotent: avoid queueing duplicates if an activation email is already pending.
        if (!await outbox.HasPendingAsync(user.Id, ActivationTokenType.AccountActivation, cancellationToken))
        {
            await outbox.EnqueueAsync(
                EmailOutboxItem.Create(
                    user.Id, user.Email, ActivationTokenType.AccountActivation, clock.GetUtcNow().UtcDateTime),
                cancellationToken);
        }

        return ResendActivationOutcome.Queued;
    }
}