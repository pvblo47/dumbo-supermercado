using Dumbo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dumbo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the <c>activation_tokens</c> table mapping using index and foreign-key names
/// aligned with the shared SQL schema.
/// </summary>
public sealed class ActivationTokenConfiguration : IEntityTypeConfiguration<ActivationToken>
{
    /// <summary>
    /// Configures columns, check constraints, indexes, and relationships for <see cref="ActivationToken"/>.
    /// </summary>
    public void Configure(EntityTypeBuilder<ActivationToken> builder)
    {
        builder.ToTable("activation_tokens", t =>
            t.HasCheckConstraint("ck_activation_tokens_type", "type IN (1, 2)"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token).HasMaxLength(255).IsRequired();   // SHA-256 hash (64 hex chars)
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();

        builder.HasIndex(x => x.Token).IsUnique().HasDatabaseName("uq_tokens_token");
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_activation_tokens_user_id");

        // RESTRICT instead of CASCADE: user accounts are never physically deleted (D07).
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_tokens_user")
            .OnDelete(DeleteBehavior.Restrict);
    }
}