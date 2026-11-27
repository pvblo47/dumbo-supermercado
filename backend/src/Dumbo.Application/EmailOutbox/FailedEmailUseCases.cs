using Dumbo.Application.Abstractions;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.EmailOutbox;

/// <summary>
/// Administrator projection of a failed outbox email. <see cref="LastError"/> contains a sanitized
/// diagnostic summary stripped of links and tokens by <c>EmailOutboxItem.RegisterFailure</c>
/// </summary>
public sealed record FailedEmailSummary(
    Guid Id, Guid UserId, string ToEmail, string Kind, int AttemptCount,
    string? LastError, DateTime CreatedAt, DateTime? FirstFailedAt);

/// <summary>
/// Lists activation or recovery emails that exhausted all 4 delivery attempts (D06)
/// </summary>
public sealed class ListFailedEmailsUseCase(IEmailOutboxStore outbox)
{
    /// <summary>
    /// Maximum number of rows returned, prioritizing the most recent failures
    /// </summary>
    public const int MaxItems = 100;

    public async Task<IReadOnlyList<FailedEmailSummary>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var items = await outbox.ListFailedAsync(MaxItems, cancellationToken);
        return items
            .Select(i => new FailedEmailSummary(
                i.Id, i.UserId, i.ToEmail, i.Kind.ToString(), i.AttemptCount,
                i.LastError, i.CreatedAt, i.FirstFailedAt))
            .ToList();
    }
}

public enum RetryFailedEmailOutcome { Requeued, NotFound, NotFailed }

/// <summary>
/// Manually requeues an exhausted outbox email for an administrator. Only re-enqueues the
/// message; delivery is performed by the worker, which first verifies that the account is still in
/// the expected state (activation => pending; recovery => active) and cancels it otherwise without sending.
/// </summary>
public sealed class RetryFailedEmailUseCase(IEmailOutboxStore outbox, TimeProvider clock)
{
    public async Task<RetryFailedEmailOutcome> ExecuteAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var item = await outbox.FindAsync(emailId, cancellationToken);
        if (item is null)
            return RetryFailedEmailOutcome.NotFound;

        // Only Failed items may be requeued; retrying Pending, Sent, or Cancelled items could duplicate delivery.
        if (item.Status != EmailOutboxStatus.Failed)
            return RetryFailedEmailOutcome.NotFailed;

        item.Requeue(clock.GetUtcNow().UtcDateTime);
        await outbox.SaveChangesAsync(cancellationToken);
        return RetryFailedEmailOutcome.Requeued;
    }
}