using Dumbo.Domain.Entities;

namespace Dumbo.Application.Abstractions;

/// <summary>
/// Read-only queries over accounts. Used by sign-in and account status revalidation
/// on each protected request.
/// Returns accounts in any status: deciding how to handle a disabled or deleted account
/// is the use case's responsibility, not the store's.
/// </summary>
public interface IAccountLookup
{
    /// <summary>Finds an account by an already normalized email (trimmed and lowercased).</summary>
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Finds an account by internal identifier. Returns null if not found.</summary>
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}