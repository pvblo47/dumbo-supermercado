using Dumbo.Domain.Entities;

namespace Dumbo.Application.Abstractions;

/// <summary>
/// Defines persistence operations for user accounts within the application layer,
/// decoupling business logic from database-specific implementations.
/// </summary>
public interface IAccountStore
{
    /// <summary>
    /// Checks whether a normalized email is already registered in the system.
    /// </summary>
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to add a new user. Returns false if another account has reserved the email 
    /// (enforced by database unique constraints and lifecycle preservation rules D07.6).
    /// </summary>
    /// <remarks>When supplied, the pending email and account are inserted in the same transaction.</remarks>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken, EmailOutboxItem? pendingEmail = null);
}
