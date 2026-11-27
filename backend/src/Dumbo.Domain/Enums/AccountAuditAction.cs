namespace Dumbo.Domain.Enums;

/// <summary>
/// Account lifecycle actions recorded for audit traceability
/// </summary>
public enum AccountAuditAction
{
    /// <summary>
    /// Soft-deletion of the account
    /// </summary>
    Deleted = 1,

    /// <summary>
    /// Restoration of a deleted account (returns in a Disabled state)
    /// </summary>
    Restored = 2
}