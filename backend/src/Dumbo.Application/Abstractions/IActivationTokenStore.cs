using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Abstractions;

/// <summary>
/// Persists single-use activation and password-recovery tokens (backed by the activation_tokens table).
/// </summary>
public interface IActivationTokenStore
{
    /// <summary>
    /// Finds a token record by its hashed value (never by the raw token), including the owning user account.
    /// </summary>
    Task<ActivationToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Issues a new token in a single transaction: expires prior unused tokens of the same type
    /// for the account and persists the new 30-minute token.
    /// </summary>
    Task ReplaceAsync(Guid userId, ActivationTokenType type, string tokenHash, DateTime now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically redeems the token once, sets the updated password hash and target account status,
    /// and expires any remaining unused tokens for the account.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> if the token stopped being valid or the account changed state or session version.
    /// </returns>
    Task<bool> RedeemAsync(Guid tokenId, Guid userId, string passwordHash, UserStatus expectedStatus,
        Guid expectedSessionVersion,
        DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Immediately expires all active activation and recovery tokens for the specified account,
    /// typically invoked when soft-deleting an account
    /// </summary>
    Task ExpireAllAsync(Guid userId, DateTime now, CancellationToken cancellationToken);
}
