using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dumbo.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IClientAdminStore"/> centralizing the non-deleted
/// <see cref="UserRole.Client"/> scope so callers cannot accidentally access staff or deleted accounts.
/// </summary>
internal sealed class ClientAdminStore(DumboDbContext db) : IClientAdminStore
{
    private IQueryable<User> VigentClients =>
        db.Users.Where(u => u.Role == UserRole.Client && u.Status != UserStatus.Deleted);

    public async Task<ClientQueryResult> ListAsync(ClientQuery query, CancellationToken cancellationToken)
    {
        var clients = VigentClients.AsNoTracking();

        if (query.Status is { } status)
            clients = clients.Where(u => u.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Case-insensitive PostgreSQL search with %, _, and \ escaped so input is matched literally.
            var pattern = $"%{EscapeLike(query.Search)}%";
            clients = clients.Where(u =>
                EF.Functions.ILike(u.Email, pattern, "\\") ||
                EF.Functions.ILike(u.FirstName, pattern, "\\") ||
                EF.Functions.ILike(u.LastName, pattern, "\\") ||
                EF.Functions.ILike(u.Phone, pattern, "\\"));
        }

        var total = await clients.CountAsync(cancellationToken);

        // Tie-break by Id to guarantee deterministic pagination across pages.
        var items = await clients
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new ClientQueryResult(items, total);
    }

    public Task<User?> FindTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        VigentClients.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "uq_users_email"
        })
        {
            // Another concurrent request claimed the email before SaveChanges completed.
            return false;
        }
    }

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}