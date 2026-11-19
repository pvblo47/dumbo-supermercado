using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Dumbo.Domain.Rules;

namespace Dumbo.Application.Accounts;

/// <summary>
/// Command payload for self-registering a new client account.
/// Excludes role and RUT: self-registration strictly creates Client accounts.
/// </summary>
public sealed record RegisterClientCommand(
    string? Email, string? FirstName, string? LastName, string? Phone, string? Password);

/// <summary>
/// Read-only summary representation of a user account returned after operations.
/// </summary>
public sealed record AccountSummary(
    Guid Id, string Email, string FirstName, string LastName, string Phone, string Role, string Status);

/// <summary>
/// Represents the outcome results of a client registration attempt.
/// </summary>
public enum RegisterClientOutcome { Created, Invalid, EmailTaken }

/// <summary>
/// Encapsulates the result data of a registration execution, including outcomes, 
/// summaries, or structured validation errors.
/// </summary>
public sealed record RegisterClientResult(
    RegisterClientOutcome Outcome,
    AccountSummary? Account = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>
/// Use case responsible for orchestrating the self-registration flow for new clients,
/// enforcing input sanitization, domain business rules, and persistence concurrency checks.
/// </summary>
public sealed class RegisterClientUseCase(
    IAccountStore store, IAccountPasswordHasher hasher, TimeProvider clock)
{
    public const string EmailTakenMessage = "El correo ya está registrado";
    public const int MaxNameLength = 100;
    public const int MaxPhoneLength = 50;

    /// <summary>
    /// Executes the client registration process asynchronously.
    /// </summary>
    public async Task<RegisterClientResult> ExecuteAsync(
        RegisterClientCommand command, CancellationToken cancellationToken)
    {
        var firstName = command.FirstName?.Trim();
        var lastName = command.LastName?.Trim();
        var phone = command.Phone?.Trim();
        var email = string.IsNullOrWhiteSpace(command.Email) ? null : AccountRules.NormalizeEmail(command.Email);

        var errors = new Dictionary<string, string[]>();

        Require(errors, "firstName", firstName, "Los nombres son obligatorios", MaxNameLength);
        Require(errors, "lastName", lastName, "Los apellidos son obligatorios", MaxNameLength);
        Require(errors, "phone", phone, "El teléfono es obligatorio", MaxPhoneLength);

        if (email is null)
            errors["email"] = ["El correo es obligatorio"];
        else if (!AccountRules.IsValidEmail(email))
            errors["email"] = ["El correo no tiene un formato válido"];

        if (!AccountRules.IsValidPassword(command.Password))
            errors["password"] = [AccountRules.WeakPasswordMessage];

        if (errors.Count > 0)
            return new RegisterClientResult(RegisterClientOutcome.Invalid, Errors: errors);

        if (await store.EmailExistsAsync(email!, cancellationToken))
            return EmailTaken();

        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Email = email!,
            FirstName = firstName!,
            LastName = lastName!,
            Phone = phone!,
            PasswordHash = hasher.Hash(command.Password!),
            Role = UserRole.Client,
            Status = UserStatus.Active,
            CreatedAt = now,
        };

        // Concurrent race condition handling: if two requests register the same email simultaneously, 
        // database constraints ensure only one succeeds; the other returns EmailTaken.
        if (!await store.TryAddAsync(user, cancellationToken))
            return EmailTaken();

        return new RegisterClientResult(
            RegisterClientOutcome.Created,
            new AccountSummary(user.Id, user.Email, user.FirstName, user.LastName, user.Phone,
                user.Role.ToString(), user.Status.ToString()));
    }

    private static RegisterClientResult EmailTaken() =>
        new(RegisterClientOutcome.EmailTaken,
            Errors: new Dictionary<string, string[]> { ["email"] = [EmailTakenMessage] });

    /// <summary>
    /// Validates that a string field is present, non-empty, and respects the database column length limits.
    /// </summary>
    private static void Require(
        Dictionary<string, string[]> errors, string field, string? value, string requiredMessage, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            errors[field] = [requiredMessage];
        else if (value.Length > maxLength)
            errors[field] = [$"No puede superar {maxLength} caracteres"];
    }
}