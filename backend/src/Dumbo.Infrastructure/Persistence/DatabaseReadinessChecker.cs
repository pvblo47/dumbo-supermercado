using Dumbo.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dumbo.Infrastructure.Persistence;

internal sealed class DatabaseReadinessChecker(DumboDbContext db) : IDatabaseReadinessChecker
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        db.Database.CanConnectAsync(cancellationToken);
}
