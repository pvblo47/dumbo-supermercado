using Dumbo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dumbo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core type configuration for the <see cref="User"/> entity,
/// mapping table structure, check constraints, column lengths, and unique indexes.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Configures the database schema mapping for <see cref="User"/>.
    /// </summary>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t =>
        {
            t.HasCheckConstraint("ck_users_role", "role IN (1, 2, 3)");
            t.HasCheckConstraint("ck_users_status", "status IN (1, 2, 3, 4)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(255);

        builder.Property(x => x.Role).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.SessionVersion).IsConcurrencyToken();

        // Same index name as the shared SQL schema; used by AccountStore to detect duplicate emails
        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("uq_users_email");
    }
}
