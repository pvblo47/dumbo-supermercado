using Dumbo.Application.Abstractions;
using Dumbo.Application.Email;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Accounts;

/// <summary>
/// Use case for dispatching due emails from the outbox queue (D06), invoked periodically by a background worker.
/// Issues a fresh link token on each attempt so expired links are never resent and prior tokens are invalidated,
/// without persisting raw tokens anywhere.
/// </summary>
public sealed class ProcessEmailOutboxUseCase(
    IEmailOutboxStore outbox,
    IAccountLookup lookup,
    IActivationTokenStore tokens,
    IEmailSender sender,
    EmailLinkSettings links,
    TimeProvider clock)
{
    /// <summary>
    /// Fetches up to <paramref name="batchSize"/> due outbox items and processes each one sequentially.
    /// </summary>
    /// <returns>The number of due outbox items that were processed in this batch.</returns>
    public async Task<int> ExecuteAsync(int batchSize, CancellationToken cancellationToken)
    {
        var due = await outbox.GetDueAsync(clock.GetUtcNow().UtcDateTime, batchSize, cancellationToken);

        foreach (var item in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessAsync(item, cancellationToken);
        }

        return due.Count;
    }

    private async Task ProcessAsync(EmailOutboxItem item, CancellationToken cancellationToken)
    {
        var user = await lookup.FindByIdAsync(item.UserId, cancellationToken);

        // Cancel without sending if the account state changed after queuing
        // (e.g., disabled, deleted, or already activated).
        if (user is null || !IsStillEligible(item.Kind, user.Status))
        {
            item.Cancel();
            await outbox.SaveChangesAsync(cancellationToken);
            return;
        }

        var (rawToken, tokenHash) = SecureToken.Generate();
        await tokens.ReplaceAsync(user.Id, item.Kind, tokenHash, clock.GetUtcNow().UtcDateTime, cancellationToken);

        var message = AccountEmailTemplates.Build(item.Kind, user.FirstName, user.Email, BuildLink(item.Kind, rawToken));

        try
        {
            await sender.SendAsync(message, cancellationToken);
            item.MarkSent(clock.GetUtcNow().UtcDateTime);
        }
        catch (EmailDeliveryException ex)
        {
            item.RegisterFailure(clock.GetUtcNow().UtcDateTime, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Persist only the exception type name so unexpected error messages cannot leak sensitive data.
            item.RegisterFailure(clock.GetUtcNow().UtcDateTime, ex.GetType().Name);
        }

        await outbox.SaveChangesAsync(cancellationToken);
    }

    private static bool IsStillEligible(ActivationTokenType kind, UserStatus status) => kind switch
    {
        ActivationTokenType.AccountActivation => status == UserStatus.PendingActivation,
        ActivationTokenType.PasswordReset => status == UserStatus.Active,
        _ => false
    };

    // Angular frontend routes that read "token" from the query string and call the backend API.
    private string BuildLink(ActivationTokenType kind, string rawToken)
    {
        var path = kind == ActivationTokenType.AccountActivation ? "activar-cuenta" : "restablecer-contrasena";
        return $"{links.PublicBaseUrl.TrimEnd('/')}/{path}?token={Uri.EscapeDataString(rawToken)}";
    }
}