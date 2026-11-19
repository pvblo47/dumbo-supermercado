using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dumbo.Infrastructure.Persistence;

/// <summary>
/// Implements <see cref="IAccountLookup"/> using EF Core. Uses AsNoTracking because it is
/// read-only: EF does not need to track changes on the returned entities.
/// </summary>
internal sealed class AccountLookup(DumboDbContext db) : IAccountLookup
{
    /// <summary>Finds a user by normalized email without change tracking.</summary>
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    /// <summary>Finds a user by identifier without change tracking.</summary>
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}