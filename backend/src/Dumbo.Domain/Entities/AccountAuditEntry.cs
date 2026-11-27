using Dumbo.Domain.Enums;

namespace Dumbo.Domain.Entities;

/// <summary>
/// Audit trail entry recording a soft-deletion or restoration of an account
/// Stores who performed the action (actor), which account was affected (target), the action,
/// timestamp, and previous/new statuses. Never stores passwords or tokens, and is append-only.
/// </summary>
public sealed class AccountAuditEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Identifier of the account being modified
    /// </summary>
    public Guid TargetUserId { get; set; }

    /// <summary>
    /// Identifier of the Worker or Admin account performing the action
    /// </summary>
    public Guid ActorUserId { get; set; }

    public AccountAuditAction Action { get; set; }

    public UserStatus PreviousStatus { get; set; }

    public UserStatus NewStatus { get; set; }

    /// <summary>
    /// UTC timestamp when the action occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }
}