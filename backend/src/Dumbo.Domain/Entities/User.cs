using Dumbo.Domain.Enums;

namespace Dumbo.Domain.Entities;

/// <summary>
/// System user account: identified by a stable internal ID and a unique normalized email.
/// Never physically deleted: the email remains reserved across all lifecycle states (D07.6).
/// </summary>
public sealed class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Null while the account is in PendingActivation status
    /// </summary>
    public string? PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Client;
    public UserStatus Status { get; set; } = UserStatus.PendingActivation;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}