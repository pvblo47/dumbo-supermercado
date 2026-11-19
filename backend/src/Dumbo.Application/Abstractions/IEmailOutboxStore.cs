using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Abstractions;

/// <summary>
/// Persists outbound email queue items (backed by the email_outbox table).
/// </summary>
public interface IEmailOutboxStore
{
    /// <summary>
    /// Checks whether a pending email of the specified type already exists for the account to prevent duplicate queuing.
    /// </summary>
    Task<bool> HasPendingAsync(Guid userId, ActivationTokenType kind, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new email item to the outbox queue.
    /// </summary>
    Task EnqueueAsync(EmailOutboxItem item, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves pending email items whose next scheduled attempt is due at or before <paramref name="now"/>,
    /// ordered from oldest to newest.
    /// </summary>
    Task<IReadOnlyList<EmailOutboxItem>> GetDueAsync(DateTime now, int max, CancellationToken cancellationToken);

    /// <summary>
    /// Persists state updates made to items retrieved via <see cref="GetDueAsync"/>.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}