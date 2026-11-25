using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Clients;

/// <summary>
/// Full profile and email update command for a client account
/// </summary>
public sealed record UpdateClientCommand(
    Guid Id, string? Email, string? FirstName, string? LastName, string? Phone);

/// <summary>
/// Updates a client's profile fields and email address (RF-007), rejecting emails already
/// used by another account while allowing unchanged identifiers (RF-002).
/// </summary>
public sealed class UpdateClientUseCase(
    IClientAdminStore store, IAccountStore accounts, IEmailOutboxStore outbox, TimeProvider clock)
{
    /// <summary>
    /// Validates client data, persists changes, and queues a new activation email when a pending client's email changes.
    /// </summary>
    public async Task<UpdateClientResult> ExecuteAsync(UpdateClientCommand command, CancellationToken cancellationToken)
    {
        var (data, errors) = ClientDataValidator.Validate(
            command.Email, command.FirstName, command.LastName, command.Phone);
        if (data is null)
            return new UpdateClientResult(UpdateClientOutcome.Invalid, Errors: errors);

        var client = await store.FindTrackedAsync(command.Id, cancellationToken);
        if (client is null)
            return new UpdateClientResult(UpdateClientOutcome.NotFound);

        // Only check uniqueness when the email actually changes so the account does not conflict with itself
        var emailChanged = data.Email != client.Email;
        if (emailChanged && await accounts.EmailExistsAsync(data.Email, cancellationToken))
            return EmailTaken();

        client.Email = data.Email;
        client.FirstName = data.FirstName;
        client.LastName = data.LastName;
        client.Phone = data.Phone;
        client.UpdatedAt = clock.GetUtcNow().UtcDateTime;

        // Save the account first: enqueueing an email shares the same DbContext and would otherwise
        // flush entity changes before unique-email conflict handling runs
        if (!await store.TrySaveAsync(cancellationToken))
            return EmailTaken();

        // For pending accounts whose email changed, queue an activation email to the new address
        // (issuing a new token invalidates prior ones, and the worker reads the current account email).
        if (emailChanged && client.Status == UserStatus.PendingActivation
            && !await outbox.HasPendingAsync(client.Id, ActivationTokenType.AccountActivation, cancellationToken))
        {
            await outbox.EnqueueAsync(
                EmailOutboxItem.Create(
                    client.Id, client.Email, ActivationTokenType.AccountActivation, client.UpdatedAt.Value),
                cancellationToken);
        }

        return new UpdateClientResult(UpdateClientOutcome.Updated, client.ToSummary());
    }

    private static UpdateClientResult EmailTaken() =>
        new(UpdateClientOutcome.EmailTaken,
            Errors: new Dictionary<string, string[]> { ["email"] = [RegisterClientUseCase.EmailTakenMessage] });
}