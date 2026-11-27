using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dumbo.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of <see cref="IAccountStore"/> backed by PostgreSQL.
/// </summary>
internal sealed class AccountStore(DumboDbContext db) : IAccountStore
{
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

    /// <summary>
    /// Attempts to persist a new user account, handling unique email constraint violations
    /// caused by concurrent registrations and detaching the failed entity from the change tracker.
    /// </summary>
    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken, EmailOutboxItem? pendingEmail = null)
    {
        db.Users.Add(user);
        if (pendingEmail is not null)
            db.EmailOutbox.Add(pendingEmail);
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
            db.Entry(user).State = EntityState.Detached;
            if (pendingEmail is not null)
                db.Entry(pendingEmail).State = EntityState.Detached;
            return false;
        }
    }
}
