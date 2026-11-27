using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dumbo.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IClientAdminStore"/> supporting both client and worker
/// administration by scoping queries to the requested <see cref="UserRole"/> in one place.
/// </summary>
internal sealed class ClientAdminStore(DumboDbContext db, IActivationTokenStore tokens) : IClientAdminStore
{
    // Scopes queries to the target role, optionally including soft-deleted accounts.
    private IQueryable<User> Scope(UserRole role, bool includeDeleted) =>
        db.Users.Where(u => u.Role == role && (includeDeleted || u.Status != UserStatus.Deleted));

    public async Task<ClientQueryResult> ListAsync(ClientQuery query, CancellationToken cancellationToken)
    {
        // Deleted view returns only Deleted accounts; standard listing excludes Deleted accounts
        var accounts = Scope(query.Role, includeDeleted: true).AsNoTracking();
        accounts = query.Deleted
            ? accounts.Where(u => u.Status == UserStatus.Deleted)
            : accounts.Where(u => u.Status != UserStatus.Deleted);

        if (!query.Deleted && query.Status is { } status)
            accounts = accounts.Where(u => u.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Case-insensitive PostgreSQL pattern match with %, _, and \ escaped for literal search.
            var pattern = $"%{EscapeLike(query.Search)}%";
            accounts = accounts.Where(u =>
                EF.Functions.ILike(u.Email, pattern, "\\") ||
                EF.Functions.ILike(u.FirstName, pattern, "\\") ||
                EF.Functions.ILike(u.LastName, pattern, "\\") ||
                EF.Functions.ILike(u.Phone, pattern, "\\"));
        }

        var total = await accounts.CountAsync(cancellationToken);

        // Tie-break by Id so rows never duplicate or skip across pages.
        var items = await accounts
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new ClientQueryResult(items, total);
    }

    public Task<User?> FindTrackedAsync(
        Guid id, CancellationToken cancellationToken, UserRole role, bool includeDeleted) =>
        Scope(role, includeDeleted).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<AccountSaveOutcome> SaveAsync(CancellationToken cancellationToken, EmailOutboxItem? pendingEmail,
        AccountAuditEntry? audit, User? invalidateLinksFor)
    {
        if (pendingEmail is not null)
            db.EmailOutbox.Add(pendingEmail);
        if (audit is not null)
            db.AccountAudit.Add(audit);

        await using var transaction = invalidateLinksFor is null
            ? null : await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (invalidateLinksFor is not null)
            {
                await tokens.ExpireAllAsync(invalidateLinksFor.Id, invalidateLinksFor.UpdatedAt!.Value, cancellationToken);
                await transaction!.CommitAsync(cancellationToken);
            }
            return AccountSaveOutcome.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            return AccountSaveOutcome.ChangedConcurrently;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "uq_users_email"
        })
        {
            // Another concurrent request claimed the email between the check and SaveChanges.
            return AccountSaveOutcome.EmailTaken;
        }
    }

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
