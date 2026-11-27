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
internal sealed class ClientAdminStore(DumboDbContext db) : IClientAdminStore
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

    public void AddAudit(AccountAuditEntry entry) => db.AccountAudit.Add(entry);

    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Persist status changes and audit entries atomically in a single SaveChanges call.
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "uq_users_email"
        })
        {
            // Another concurrent request claimed the email between the check and SaveChanges.
            return false;
        }
    }

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}