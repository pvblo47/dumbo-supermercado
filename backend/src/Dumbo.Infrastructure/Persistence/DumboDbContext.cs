using Dumbo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dumbo.Infrastructure.Persistence;

public sealed class DumboDbContext(DbContextOptions<DumboDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ActivationToken> ActivationTokens => Set<ActivationToken>();
    public DbSet<EmailOutboxItem> EmailOutbox => Set<EmailOutboxItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DumboDbContext).Assembly);

        //Recordatorio mio: añadir el snakecase
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(SnakeCase.Convert(property.Name));
    }
}
