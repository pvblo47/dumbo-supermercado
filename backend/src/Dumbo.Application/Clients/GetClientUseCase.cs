using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Clients;

/// <summary>
/// Retrieves a non-deleted client account by ID to populate the staff edit form
/// </summary>
public sealed class GetClientUseCase(IAccountLookup lookup)
{
    /// <summary>
    /// Finds a non-deleted <see cref="UserRole.Client"/> account by identifier
    /// </summary>
    /// <returns>
    /// The account summary, or <see langword="null"/> if not found, deleted, or not a client
    /// </returns>
    public async Task<AccountSummary?> ExecuteAsync(Guid id, CancellationToken cancellationToken, UserRole role = UserRole.Client)
    {
        var user = await lookup.FindByIdAsync(id, cancellationToken);
        return user is not null && user.Role == role && user.Status != UserStatus.Deleted ? user.ToSummary() : null;
    }
}