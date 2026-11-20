using Dumbo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dumbo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the <c>email_outbox</c> queue table mapping (newly introduced alongside the shared SQL schema).
/// </summary>
public sealed class EmailOutboxItemConfiguration : IEntityTypeConfiguration<EmailOutboxItem>
{
    /// <summary>
    /// Configures columns, check constraints, indexes, and relationships for <see cref="EmailOutboxItem"/>.
    /// </summary>
    public void Configure(EntityTypeBuilder<EmailOutboxItem> builder)
    {
        builder.ToTable("email_outbox", t =>
        {
            t.HasCheckConstraint("ck_email_outbox_kind", "kind IN (1, 2)");
            t.HasCheckConstraint("ck_email_outbox_status", "status IN (1, 2, 3, 4)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ToEmail).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Kind).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(500);

        // Supports background polling for pending items whose next attempt time is due.
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt }).HasDatabaseName("ix_email_outbox_status_next_attempt");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_email_outbox_user")
            .OnDelete(DeleteBehavior.Restrict);
    }
}