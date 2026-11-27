using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Abstractions;

/// <summary>
/// Pre-validated and normalized query filter for listing client accounts; implementations execute the query directly.
/// </summary>
/// <param name="Search">Search text matched against email, given name, family name, and phone; <see langword="null"/> means no filter.</param>
/// <param name="Status">Exact account status filter; <see langword="null"/> includes all active/non-deleted accounts (never <see cref="UserStatus.Deleted"/>).</param>
/// <param name="Page">1-based page index.</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record ClientQuery(string? Search, UserStatus? Status, int Page, int PageSize, UserRole Role = UserRole.Client, bool Deleted = false);

/// <summary>
/// Represents a paged slice of client records along with the total matching count across all pages.
/// </summary>
public sealed record ClientQueryResult(IReadOnlyList<User> Items, int Total);

public enum AccountSaveOutcome { Saved, EmailTaken, ChangedConcurrently }

/// <summary>
/// Persistence abstraction for staff client administration (RF-007), scoped strictly to non-deleted <see cref="UserRole.Client"/> accounts.
/// </summary>
public interface IClientAdminStore
{
    /// <summary>Returns a page of non-deleted clients ordered by family name and given name.</summary>
    Task<ClientQueryResult> ListAsync(ClientQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a tracked account for modification prior to <see cref="SaveAsync"/>,
    /// or <see langword="null"/> if not found, deleted, or not a client.
    /// </summary>
    Task<User?> FindTrackedAsync(Guid id, CancellationToken cancellationToken, UserRole role = UserRole.Client, bool includeDeleted = false);

    /// <summary>
    /// Saves the account together with its audit, queued email or invalidation of links when requested.
    /// </summary>
    Task<AccountSaveOutcome> SaveAsync(CancellationToken cancellationToken, EmailOutboxItem? pendingEmail = null,
        AccountAuditEntry? audit = null, User? invalidateLinksFor = null);
}
