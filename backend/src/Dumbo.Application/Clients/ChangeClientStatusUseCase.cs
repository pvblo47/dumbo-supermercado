using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Clients;

/// <summary>
/// Enables or disables a <see cref="UserRole.Client"/> or <see cref="UserRole.Worker"/> account (RF-007, RF-003, D07).
/// Disabling revokes access immediately because protected endpoints re-evaluate account status per request (NF02),
/// causing even unexpired tokens to return 401. The operation is idempotent and returns the current summary when unchanged.
/// </summary>
public sealed class ChangeClientStatusUseCase(IClientAdminStore store, IEmailOutboxStore outbox, TimeProvider clock)
{
    public async Task<ChangeClientStatusResult> ExecuteAsync(
        Guid accountId, ClientStatusAction action, CancellationToken cancellationToken,
        UserRole role = UserRole.Client)
    {
        var account = await store.FindTrackedAsync(accountId, cancellationToken, role);
        if (account is null)
            return new ChangeClientStatusResult(ChangeClientStatusOutcome.NotFound);

        var now = clock.GetUtcNow().UtcDateTime;
        var resendActivation = false;

        var target = action switch
        {
            ClientStatusAction.Disable => UserStatus.Disabled,

            // Enabling applies only to Disabled accounts.
            // 6 enabling requires completed activation; if no password was set, move to PendingActivation
            // and queue a replacement activation link instead of activating directly.
            _ when account.Status != UserStatus.Disabled => account.Status,
            _ when account.PasswordHash is null => Pending(out resendActivation),
            _ => UserStatus.Active
        };

        if (target != account.Status)
        {
            account.Status = target;
            account.UpdatedAt = now;
            await store.TrySaveAsync(cancellationToken);   // Persist status change before enqueueing outbox item
        }

        if (resendActivation
            && !await outbox.HasPendingAsync(account.Id, ActivationTokenType.AccountActivation, cancellationToken))
        {
            await outbox.EnqueueAsync(
                EmailOutboxItem.Create(account.Id, account.Email, ActivationTokenType.AccountActivation, now),
                cancellationToken);
        }

        return new ChangeClientStatusResult(ChangeClientStatusOutcome.Changed, account.ToSummary());

        static UserStatus Pending(out bool resend)
        {
            resend = true;
            return UserStatus.PendingActivation;
        }
    }
}