using Microsoft.EntityFrameworkCore;

namespace Dumbo.Infrastructure.Persistence;

public sealed class DumboDbContext(DbContextOptions<DumboDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DumboDbContext).Assembly);
    }
}
