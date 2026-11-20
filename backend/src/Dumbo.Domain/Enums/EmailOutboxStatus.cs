namespace Dumbo.Domain.Enums;

/// <summary>
/// Represents the delivery lifecycle state of an email item in the outbox queue (D06).
/// </summary>
public enum EmailOutboxStatus
{
    /// <summary>
    /// Awaiting its initial delivery attempt or a scheduled retry.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Delivered to the email provider; no further retries will be attempted.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Matches the ERS "Error de envío" state after exhausting all 4 automatic delivery attempts.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Discarded because the target account state changed before delivery (custom workflow safeguard).
    /// </summary>
    Cancelled = 4
}