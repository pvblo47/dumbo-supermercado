using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dumbo.Infrastructure.Persistence;

/// <summary>
/// Implements <see cref="IActivationTokenStore"/> using Entity Framework Core.
/// Multi-row state transitions execute within explicit database transactions to prevent partial updates.
/// </summary>
internal sealed class ActivationTokenStore(DumboDbContext db) : IActivationTokenStore
{
    /// <inheritdoc />
    public Task<ActivationToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.ActivationTokens.AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);

    /// <inheritdoc />
    public async Task ReplaceAsync(
        Guid userId, ActivationTokenType type, string tokenHash, DateTime now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Immediately expire any prior unused tokens of the same type so previous links are invalidated.
        await db.ActivationTokens
            .Where(t => t.UserId == userId && t.Type == type && !t.IsUsed && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, now), cancellationToken);

        db.ActivationTokens.Add(new ActivationToken
        {
            UserId = userId,
            Type = type,
            Token = tokenHash,
            CreatedAt = now,
            ExpiresAt = now + ActivationToken.Lifetime
        });
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RedeemAsync(
        Guid tokenId, Guid userId, string passwordHash, UserStatus expectedStatus, Guid expectedSessionVersion, DateTime now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Lock the user before its tokens, in the same order as account lifecycle updates.
        var nextSessionVersion = Guid.NewGuid();
        var updated = await db.Users
            .Where(u => u.Id == userId && u.Status == expectedStatus && u.SessionVersion == expectedSessionVersion)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.PasswordHash, passwordHash)
                      .SetProperty(u => u.Status, UserStatus.Active)
                      .SetProperty(u => u.SessionVersion, nextSessionVersion)
                      .SetProperty(u => u.UpdatedAt, now), cancellationToken);

        if (updated == 0)
            return false;

        var consumed = await db.ActivationTokens
            .Where(t => t.Id == tokenId && t.UserId == userId && !t.IsUsed && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.IsUsed, true).SetProperty(t => t.UsedAt, now), cancellationToken);

        if (consumed == 0)
            return false;   // Exiting without Commit automatically rolls back the transaction.

        // Expire any remaining unused tokens belonging to the same account.
        await db.ActivationTokens
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, now), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public Task ExpireAllAsync(Guid userId, DateTime now, CancellationToken cancellationToken) =>
        db.ActivationTokens
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, now), cancellationToken);
}
