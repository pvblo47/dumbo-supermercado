using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Api.Tests;

/// <summary>
/// In-memory <see cref="IAccountStore"/> backed by <see cref="FakeAccountLookup.Users"/> so client mutations remain visible to auth and session checks
/// </summary>
internal sealed class LookupBackedAccountStore(FakeAccountLookup users, InMemoryOutbox outbox) : IAccountStore
{
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(users.Users.Any(u => u.Email == normalizedEmail));

    public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken, EmailOutboxItem? pendingEmail = null)
    {
        if (users.Users.Any(u => u.Email == user.Email))
            return Task.FromResult(false);

        users.Users.Add(user);
        if (pendingEmail is not null)
            outbox.Items.Add(pendingEmail);
        return Task.FromResult(true);
    }
}

/// <summary>
/// In-memory <see cref="IClientAdminStore"/> mirroring production query, ordering, search, and
/// paging behavior over non-deleted clients; returning references directly provides change tracking.
/// </summary>
internal sealed class InMemoryClientAdminStore(
    FakeAccountLookup users, InMemoryOutbox outbox, InMemoryTokenStore tokens) : IClientAdminStore
{
    /// <summary>
    /// Simulates a concurrent unique-email conflict when saving changes.
    /// </summary>
    public bool LoseRaceOnSave { get; set; }

    /// <summary>
    /// Captured audit entries recorded during lifecycle actions (D07.7) for test assertions.
    /// </summary>
    public List<AccountAuditEntry> Audit { get; } = [];

    public Task<ClientQueryResult> ListAsync(ClientQuery query, CancellationToken cancellationToken)
    {
        var accounts = users.Users.Where(u => u.Role == query.Role &&
            (query.Deleted ? u.Status == UserStatus.Deleted : u.Status != UserStatus.Deleted));

        if (!query.Deleted && query.Status is { } status)
            accounts = accounts.Where(u => u.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var text = query.Search;
            accounts = accounts.Where(u =>
                Has(u.Email, text) || Has(u.FirstName, text) || Has(u.LastName, text) || Has(u.Phone, text));
        }

        // Deterministic ordering matching database collation so pagination assertions remain stable.
        var all = accounts
            .OrderBy(u => u.LastName, StringComparer.Ordinal)
            .ThenBy(u => u.FirstName, StringComparer.Ordinal)
            .ThenBy(u => u.Id)
            .ToList();

        IReadOnlyList<User> items = all.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
        return Task.FromResult(new ClientQueryResult(items, all.Count));
    }

    public Task<User?> FindTrackedAsync(
        Guid id, CancellationToken cancellationToken, UserRole role, bool includeDeleted) =>
        Task.FromResult(users.Users.FirstOrDefault(
            u => u.Id == id && u.Role == role && (includeDeleted || u.Status != UserStatus.Deleted)));

    public async Task<AccountSaveOutcome> SaveAsync(CancellationToken cancellationToken, EmailOutboxItem? pendingEmail,
        AccountAuditEntry? audit, User? invalidateLinksFor)
    {
        if (LoseRaceOnSave)
            return AccountSaveOutcome.EmailTaken;
        if (pendingEmail is not null)
            outbox.Items.Add(pendingEmail);
        if (audit is not null)
            Audit.Add(audit);
        if (invalidateLinksFor is not null)
            await tokens.ExpireAllAsync(invalidateLinksFor.Id, invalidateLinksFor.UpdatedAt!.Value, cancellationToken);
        return AccountSaveOutcome.Saved;
    }

    private static bool Has(string value, string text) => value.Contains(text, StringComparison.OrdinalIgnoreCase);
}
