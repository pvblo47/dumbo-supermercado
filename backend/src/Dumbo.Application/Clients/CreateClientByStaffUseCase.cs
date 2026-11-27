using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Clients;

/// <summary>
/// Input payload entered by staff to provision a client account without assigning a password or role
/// </summary>
public sealed record CreateClientByStaffCommand(string? Email, string? FirstName, string? LastName, string? Phone, UserRole Role = UserRole.Client);

/// <summary>
/// Creates a <see cref="UserStatus.PendingActivation"/> client account on behalf of staff without a password,
/// letting the account holder set credentials via the activation email link (RF-002, D06).
/// </summary>
/// <remarks>
/// Enqueues the activation email into the outbox rather than sending synchronously, and reuses
/// <see cref="RegisterClientResult"/> to match self-registration outcomes.
/// </remarks>
public sealed class CreateClientByStaffUseCase(IAccountStore store, TimeProvider clock)
{
    /// <summary>
    /// Validates client data, creates a pending client account, and enqueues an activation email.
    /// </summary>
    public async Task<RegisterClientResult> ExecuteAsync(
        CreateClientByStaffCommand command, CancellationToken cancellationToken)
    {
        var (data, errors) = ClientDataValidator.Validate(
            command.Email, command.FirstName, command.LastName, command.Phone);
        if (data is null)
            return new RegisterClientResult(RegisterClientOutcome.Invalid, Errors: errors);

        if (await store.EmailExistsAsync(data.Email, cancellationToken))
            return EmailTaken();

        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Email = data.Email,
            FirstName = data.FirstName,
            LastName = data.LastName,
            Phone = data.Phone,
            PasswordHash = null,
            Role = command.Role,
            Status = UserStatus.PendingActivation,
            CreatedAt = now
        };

        var pendingEmail = EmailOutboxItem.Create(user.Id, user.Email, ActivationTokenType.AccountActivation, now);
        if (!await store.TryAddAsync(user, cancellationToken, pendingEmail))
            return EmailTaken();

        return new RegisterClientResult(RegisterClientOutcome.Created, user.ToSummary());
    }

    private static RegisterClientResult EmailTaken() =>
        new(RegisterClientOutcome.EmailTaken,
            Errors: new Dictionary<string, string[]> { ["email"] = [RegisterClientUseCase.EmailTakenMessage] });
}
