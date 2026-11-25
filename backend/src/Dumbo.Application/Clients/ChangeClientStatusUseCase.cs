using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Clients;

/// <summary>
/// Enables or disables a client account. Disabling revokes access immediately because
/// protected requests re-validate account status on every call, and operations are idempotent.
/// </summary>
public sealed class ChangeClientStatusUseCase(IClientAdminStore store, TimeProvider clock)
{
    /// <summary>
    /// Applies a status change to a non-deleted client account and returns its updated summary.
    /// </summary>
    public async Task<ChangeClientStatusResult> ExecuteAsync(
        Guid clientId, ClientStatusAction action, CancellationToken cancellationToken)
    {
        var client = await store.FindTrackedAsync(clientId, cancellationToken);
        if (client is null)
            return new ChangeClientStatusResult(ChangeClientStatusOutcome.NotFound);

        var target = action switch
        {
            ClientStatusAction.Disable => UserStatus.Disabled,

            // Only disabled accounts transition on enable; restore PendingActivation if never activated,
            // Active otherwise, and leave non-disabled accounts untouched.
            _ => client.Status == UserStatus.Disabled
                ? (client.PasswordHash is null ? UserStatus.PendingActivation : UserStatus.Active)
                : client.Status
        };

        if (target != client.Status)
        {
            client.Status = target;
            client.UpdatedAt = clock.GetUtcNow().UtcDateTime;
            await store.TrySaveAsync(cancellationToken);
        }

        return new ChangeClientStatusResult(ChangeClientStatusOutcome.Changed, client.ToSummary());
    }
}