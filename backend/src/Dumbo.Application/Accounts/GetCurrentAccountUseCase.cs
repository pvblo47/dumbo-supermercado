using Dumbo.Application.Abstractions;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Accounts;

/// <summary>
/// Use case: current account details for the caller (GET /api/auth/me).
/// Reads from the database rather than the token so returned data is always up to date.
/// </summary>
public sealed class GetCurrentAccountUseCase(IAccountLookup lookup)
{
    /// <returns>The account summary, or null if it does not exist or is no longer Active.</returns>
    public async Task<AccountSummary?> ExecuteAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var user = await lookup.FindByIdAsync(accountId, cancellationToken);
        return user is { Status: UserStatus.Active } ? user.ToSummary() : null;
    }
}