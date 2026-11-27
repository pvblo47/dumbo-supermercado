using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dumbo.Infrastructure.Persistence;

/// <summary>
/// Implements <see cref="IEmailOutboxStore"/> using Entity Framework Core.
/// Assumes a single API instance processes the queue (matching the current single-container deployment);
/// multiple instances would require row claiming or locking to prevent duplicate sends.
/// </summary>
internal sealed class EmailOutboxStore(DumboDbContext db) : IEmailOutboxStore
{
    /// <inheritdoc />
    public Task<bool> HasPendingAsync(Guid userId, ActivationTokenType kind, CancellationToken cancellationToken) =>
        db.EmailOutbox.AnyAsync(
            x => x.UserId == userId && x.Kind == kind && x.Status == EmailOutboxStatus.Pending,
            cancellationToken);

    /// <inheritdoc />
    public async Task EnqueueAsync(EmailOutboxItem item, CancellationToken cancellationToken)
    {
        db.EmailOutbox.Add(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    // Returns tracked entities so the use case can mutate state and persist via SaveChangesAsync.
    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailOutboxItem>> GetDueAsync(
        DateTime now, int max, CancellationToken cancellationToken) =>
        await db.EmailOutbox
            .Where(x => x.Status == EmailOutboxStatus.Pending && x.NextAttemptAt <= now)
            .OrderBy(x => x.NextAttemptAt)
            .Take(max)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailOutboxItem>> ListFailedAsync(int max, CancellationToken cancellationToken) =>
        await db.EmailOutbox.AsNoTracking()
            .Where(x => x.Status == EmailOutboxStatus.Failed)
            .OrderByDescending(x => x.CreatedAt)
            .Take(max)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<EmailOutboxItem?> FindAsync(Guid id, CancellationToken cancellationToken) =>db.EmailOutbox.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}