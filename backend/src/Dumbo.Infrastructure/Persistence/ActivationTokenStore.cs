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
        Guid tokenId, Guid userId, string passwordHash, UserStatus newStatus, DateTime now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Single-use enforcement: filter on unused and unexpired directly in the UPDATE statement.
        // Under concurrent requests, only one wins the row update while the other sees 0 affected rows.
        var consumed = await db.ActivationTokens
            .Where(t => t.Id == tokenId && !t.IsUsed && t.ExpiresAt > now)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.IsUsed, true).SetProperty(t => t.UsedAt, now), cancellationToken);

        if (consumed == 0)
            return false;   // Exiting without Commit automatically rolls back the transaction.

        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.PasswordHash, passwordHash)
                      .SetProperty(u => u.Status, newStatus)
                      .SetProperty(u => u.UpdatedAt, now),
                cancellationToken);

        // Expire any remaining unused tokens belonging to the same account.
        await db.ActivationTokens
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, now), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}