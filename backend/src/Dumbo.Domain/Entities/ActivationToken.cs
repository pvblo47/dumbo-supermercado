using Dumbo.Domain.Enums;

namespace Dumbo.Domain.Entities;

/// <summary>
/// Represents a single-use token link used to activate an account or reset its password (D06).
/// <see cref="Token"/> stores a SHA-256 hash rather than the raw token value, which is sent only via email.
/// </summary>
public sealed class ActivationToken
{
    /// <summary>
    /// Standard validity duration for all token links: 30 minutes (D06).
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Unique identifier for the token record.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Identifier of the user account associated with this token.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="User"/> account.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Hex-encoded SHA-256 hash of the token delivered in the email link.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Purpose of the token (account activation or password reset).
    /// </summary>
    public ActivationTokenType Type { get; set; }

    /// <summary>
    /// UTC timestamp when the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether the token has already been consumed.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Optional UTC timestamp when the token was used.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Determines whether the token is unused and not expired at the specified UTC time.
    /// </summary>
    public bool IsValidAt(DateTime utcNow) => !IsUsed && ExpiresAt > utcNow;
}