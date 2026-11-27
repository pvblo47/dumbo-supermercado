using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Clients;

public enum AccountLifecycleAction { Delete, Restore }

/// <summary>
/// Represents the outcome of a lifecycle transition. <see cref="Changed"/> includes idempotent
/// no-op deletions; <see cref="NotDeleted"/> indicates restoration was requested for a non-deleted account.
/// </summary>
public enum AccountLifecycleOutcome { Changed, NotFound, NotDeleted, ChangedConcurrently }

public sealed record AccountLifecycleResult(AccountLifecycleOutcome Outcome, AccountSummary? Account = null);

/// <summary>
/// Handles soft-deletion and restoration for <see cref="UserRole.Client"/> and <see cref="UserRole.Worker"/>
/// accounts. Soft-deleting sets <see cref="UserStatus.Deleted"/>, removes the account
/// from active listings, revokes access immediately, expires active tokens, and retains
/// the email address. Restoring returns the same account as <see cref="UserStatus.Disabled"/> rather
/// than re-enabling it automatically, recording an atomic <see cref="AccountAuditEntry"/>.
/// </summary>
public sealed class ChangeAccountLifecycleUseCase(IClientAdminStore store, TimeProvider clock)
{
    public async Task<AccountLifecycleResult> ExecuteAsync(
        Guid targetId, Guid actorId, AccountLifecycleAction action, UserRole role,
        CancellationToken cancellationToken)
    {
        // Include soft-deleted records so already-deleted accounts can be restored or idempotently deleted.
        var account = await store.FindTrackedAsync(targetId, cancellationToken, role, includeDeleted: true);
        if (account is null)
            return new AccountLifecycleResult(AccountLifecycleOutcome.NotFound);

        var now = clock.GetUtcNow().UtcDateTime;
        var previous = account.Status;

        if (action == AccountLifecycleAction.Delete)
        {
            if (previous == UserStatus.Deleted)
                return new AccountLifecycleResult(AccountLifecycleOutcome.Changed, account.ToSummary());

            // D07.4 placeholder: reject deletion once payments/reservations introduce in-flight payment states,
            // active reservations, or open payment disputes.
            account.Status = UserStatus.Deleted;
            account.DeletedAt = now;
        }
        else
        {
            if (previous != UserStatus.Deleted)
                return new AccountLifecycleResult(AccountLifecycleOutcome.NotDeleted);

            account.Status = UserStatus.Disabled;
            account.DeletedAt = null;
        }

        account.UpdatedAt = now;
        account.SessionVersion = Guid.NewGuid();
        var audit = new AccountAuditEntry
        {
            TargetUserId = account.Id,
            ActorUserId = actorId,
            Action = action == AccountLifecycleAction.Delete ? AccountAuditAction.Deleted : AccountAuditAction.Restored,
            PreviousStatus = previous,
            NewStatus = account.Status,
            OccurredAt = now
        };
        var saved = await store.SaveAsync(cancellationToken, audit: audit, invalidateLinksFor: account);
        if (saved != AccountSaveOutcome.Saved)
            return new AccountLifecycleResult(AccountLifecycleOutcome.ChangedConcurrently);

        return new AccountLifecycleResult(AccountLifecycleOutcome.Changed, account.ToSummary());
    }
}
