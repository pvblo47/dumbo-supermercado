using Dumbo.Application.Abstractions;
using Dumbo.Application.Email;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Api.Tests;

// Dobles en memoria de los tramos de enlaces y correos. Reutilizan FakeAccountLookup
// como "tabla de usuarios", para que activar o restablecer cambie la cuenta que luego usa el login.

/// <summary>Reloj que se puede mover, para probar los reintentos a los minutos 1, 5 y 15.</summary>
internal sealed class MutableClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Almacén de enlaces en memoria con las mismas reglas que el real (vencer, un solo uso).</summary>
internal sealed class InMemoryTokenStore(FakeAccountLookup users) : IActivationTokenStore
{
    public List<ActivationToken> Tokens { get; } = [];

    public Task ExpireAllAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var t in Tokens.Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > now))
            t.ExpiresAt = now;
        return Task.CompletedTask;
    }
    
    public Task<ActivationToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = Tokens.FirstOrDefault(t => t.Token == tokenHash);
        if (token is not null)
            token.User = users.Users.First(u => u.Id == token.UserId);
        return Task.FromResult(token);
    }

    public Task ReplaceAsync(Guid userId, ActivationTokenType type, string tokenHash, DateTime now,
        CancellationToken cancellationToken)
    {
        foreach (var old in Tokens.Where(t => t.UserId == userId && t.Type == type && !t.IsUsed && t.ExpiresAt > now))
            old.ExpiresAt = now;

        Tokens.Add(new ActivationToken
        {
            UserId = userId, Type = type, Token = tokenHash, CreatedAt = now, ExpiresAt = now + ActivationToken.Lifetime
        });
        return Task.CompletedTask;
    }

    public Task<bool> RedeemAsync(Guid tokenId, Guid userId, string passwordHash, UserStatus expectedStatus,
        Guid expectedSessionVersion, DateTime now, CancellationToken cancellationToken)
    {
        var token = Tokens.FirstOrDefault(t => t.Id == tokenId && t.UserId == userId && !t.IsUsed && t.ExpiresAt > now);
        var user = users.Users.FirstOrDefault(u => u.Id == userId);
        if (token is null || user is null || user.Status != expectedStatus || user.SessionVersion != expectedSessionVersion)
            return Task.FromResult(false);

        token.IsUsed = true;
        token.UsedAt = now;

        user.PasswordHash = passwordHash;
        user.Status = UserStatus.Active;
        user.SessionVersion = Guid.NewGuid();
        user.UpdatedAt = now;

        foreach (var other in Tokens.Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > now))
            other.ExpiresAt = now;

        return Task.FromResult(true);
    }
}

internal sealed class InMemoryOutbox : IEmailOutboxStore
{
    public List<EmailOutboxItem> Items { get; } = [];

    public Task<bool> HasPendingAsync(Guid userId, ActivationTokenType kind, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(i => i.UserId == userId && i.Kind == kind && i.Status == EmailOutboxStatus.Pending));

    public Task EnqueueAsync(EmailOutboxItem item, CancellationToken cancellationToken)
    {
        Items.Add(item);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EmailOutboxItem>> GetDueAsync(DateTime now, int max, CancellationToken cancellationToken)
    {
        IReadOnlyList<EmailOutboxItem> due = Items
            .Where(i => i.Status == EmailOutboxStatus.Pending && i.NextAttemptAt <= now)
            .OrderBy(i => i.NextAttemptAt)
            .Take(max)
            .ToList();
        return Task.FromResult(due);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    
    public Task<IReadOnlyList<EmailOutboxItem>> ListFailedAsync(int max, CancellationToken cancellationToken)
    {
        IReadOnlyList<EmailOutboxItem> failed = Items
            .Where(i => i.Status == EmailOutboxStatus.Failed)
            .OrderByDescending(i => i.CreatedAt)
            .Take(max)
            .ToList();
        return Task.FromResult(failed);
    }
    public Task<EmailOutboxItem?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
}

/// <summary>
/// Emisor de correos falso: guarda lo "enviado" y puede fallar las primeras N veces.
/// </summary>
internal sealed class CapturingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    /// <summary>
    /// Cuántos envíos siguientes deben fallar antes de que uno tenga éxito.
    /// </summary>
    public int FailuresToSimulate { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (FailuresToSimulate > 0)
        {
            FailuresToSimulate--;
            throw new EmailDeliveryException("Proveedor no disponible");
        }

        Sent.Add(message);
        return Task.CompletedTask;
    }

    public static string ExtractToken(EmailMessage message)
    {
        const string marker = "token=";
        var text = message.TextBody;
        var start = text.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = text.IndexOfAny(new[] { '\r', '\n', ' ' }, start);
        var raw = end < 0 ? text[start..] : text[start..end];
        return Uri.UnescapeDataString(raw);
    }
}
