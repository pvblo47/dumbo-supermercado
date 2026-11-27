using Dumbo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dumbo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the <c>account_audit</c> table for account soft-deletion and restoration traceability
/// </summary>
public sealed class AccountAuditEntryConfiguration : IEntityTypeConfiguration<AccountAuditEntry>
{
    /// <summary>
    /// Defines table check constraints, primary key, enum-to-integer conversions,
    /// history lookup index, and restrictive foreign keys to preserve audit records.
    /// </summary>
    public void Configure(EntityTypeBuilder<AccountAuditEntry> builder)
    {
        builder.ToTable("account_audit", t =>
        {
            t.HasCheckConstraint("ck_account_audit_action", "action IN (1, 2)");
            t.HasCheckConstraint("ck_account_audit_previous_status", "previous_status IN (1, 2, 3, 4)");
            t.HasCheckConstraint("ck_account_audit_new_status", "new_status IN (1, 2, 3, 4)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasConversion<int>().IsRequired();
        builder.Property(x => x.PreviousStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.NewStatus).HasConversion<int>().IsRequired();

        // Supports querying lifecycle history for a target account ordered by occurrence time.
        builder.HasIndex(x => new { x.TargetUserId, x.OccurredAt }).HasDatabaseName("ix_account_audit_target");

        // Restrict deletion so audit history remains intact even if a user record is ever hard-deleted
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.TargetUserId)
            .HasConstraintName("fk_account_audit_target").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId)
            .HasConstraintName("fk_account_audit_actor").OnDelete(DeleteBehavior.Restrict);
    }
}