using Dumbo.Domain.Enums;

namespace Dumbo.Domain.Entities;

/// <summary>
/// Represents an outbound email queued for background delivery (D06) so that transient
/// email provider failures do not break the originating user operation.
/// Does not store raw links or tokens; message content is composed at send time using a freshly issued token.
/// </summary>
public sealed class EmailOutboxItem
{
    /// <summary>
    /// Retry schedule defined by D06: 1, 5, and 15 minutes measured from the FIRST failed attempt
    /// (not from each individual failure). Combined with the initial attempt, this allows up to 4 attempts total.
    /// </summary>
    public static readonly TimeSpan[] RetryOffsets =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)];

    /// <summary>
    /// Maximum number of delivery attempts (initial attempt plus configured retries).
    /// </summary>
    public static int MaxAttempts => RetryOffsets.Length + 1;

    private const int MaxErrorLength = 500;

    /// <summary>
    /// Unique identifier for the outbox item.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Identifier of the target user account.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// Indicates the email purpose (account activation or password reset).
    /// </summary>
    public ActivationTokenType Kind { get; set; }

    /// <summary>
    /// Current delivery state of the outbox item.
    /// </summary>
    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;

    /// <summary>
    /// Total number of delivery attempts made so far.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// UTC timestamp of the first failed attempt, used as the anchor for retry offsets.
    /// </summary>
    public DateTime? FirstFailedAt { get; set; }

    /// <summary>
    /// UTC timestamp scheduled for the next attempt, or <see langword="null"/> when no further attempts remain.
    /// </summary>
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>
    /// Sanitized error summary from the most recent failure; never contains links or tokens.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// UTC timestamp when the outbox item was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the email was successfully sent.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Creates a pending outbox item scheduled for an immediate initial attempt.
    /// </summary>
    public static EmailOutboxItem Create(Guid userId, string toEmail, ActivationTokenType kind, DateTime now) =>
        new() { UserId = userId, ToEmail = toEmail, Kind = kind, CreatedAt = now, NextAttemptAt = now };

    /// <summary>
    /// Marks the item as sent and clears any pending retry schedule or prior error.
    /// </summary>
    public void MarkSent(DateTime now)
    {
        AttemptCount++;
        Status = EmailOutboxStatus.Sent;
        SentAt = now;
        NextAttemptAt = null;
        LastError = null;
    }

    /// <summary>
    /// Records a failed delivery attempt and schedules the next retry at +1, +5, or +15 minutes
    /// relative to <see cref="FirstFailedAt"/>, transitioning to <see cref="EmailOutboxStatus.Failed"/>
    /// once <see cref="MaxAttempts"/> is reached.
    /// </summary>
    public void RegisterFailure(DateTime now, string error)
    {
        AttemptCount++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        FirstFailedAt ??= now;

        if (AttemptCount >= MaxAttempts)
        {
            Status = EmailOutboxStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            NextAttemptAt = FirstFailedAt.Value + RetryOffsets[AttemptCount - 1];
        }
    }

    /// <summary>
    /// Cancels the item without sending because the account no longer meets eligibility conditions.
    /// </summary>
    public void Cancel()
    {
        Status = EmailOutboxStatus.Cancelled;
        NextAttemptAt = null;
    }

    /// <summary>
    /// Manually requeues an exhausted outbox item for immediate delivery by an administrator (D06):
    /// resets the status to <see cref="EmailOutboxStatus.Pending"/>, clears the attempt counter and
    /// initial failure timestamp, and schedules <see cref="NextAttemptAt"/> immediately. Preserves
    /// <see cref="LastError"/> so the prior failure reason remains visible until a delivery succeeds.
    /// </summary>
    public void Requeue(DateTime now)
    {
        Status = EmailOutboxStatus.Pending;
        AttemptCount = 0;
        FirstFailedAt = null;   // Reset the +1/+5/+15 min backoff schedule from zero for the new cycle
        NextAttemptAt = now;
    }
}