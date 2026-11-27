using Dumbo.Application.Accounts;
using Dumbo.Domain.Rules;

namespace Dumbo.Application.Clients;

/// <summary>
/// Represents trimmed client profile values with a normalized email address
/// </summary>
public sealed record ValidatedClientData(string Email, string FirstName, string LastName, string Phone);

/// <summary>
/// Validates and normalizes client fields for staff creation and updates using the same constraints
/// and messages as self-registration
/// </summary>
public static class ClientDataValidator
{
    /// <summary>
    /// Validates email, first name, last name, and phone inputs.
    /// </summary>
    /// <returns>
    /// The normalized <see cref="ValidatedClientData"/> with empty errors when valid; otherwise
    /// <see langword="null"/> data and per-field validation error messages.
    /// </returns>
    public static (ValidatedClientData? Data, IReadOnlyDictionary<string, string[]> Errors) Validate(
        string? email, string? firstName, string? lastName, string? phone)
    {
        var first = firstName?.Trim();
        var last = lastName?.Trim();
        var phoneText = phone?.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : AccountRules.NormalizeEmail(email);

        var errors = new Dictionary<string, string[]>();

        Require(errors, "firstName", first, "Los nombres son obligatorios", RegisterClientUseCase.MaxNameLength);
        Require(errors, "lastName", last, "Los apellidos son obligatorios", RegisterClientUseCase.MaxNameLength);
        Require(errors, "phone", phoneText, "El teléfono es obligatorio", RegisterClientUseCase.MaxPhoneLength);

        if (normalizedEmail is null)
            errors["email"] = ["El correo es obligatorio"];
        else if (!AccountRules.IsValidEmail(normalizedEmail))
            errors["email"] = ["El correo no tiene un formato válido"];

        if (errors.Count > 0)
            return (null, errors);

        return (new ValidatedClientData(normalizedEmail!, first!, last!, phoneText!), errors);
    }

    private static void Require(
        Dictionary<string, string[]> errors, string field, string? value, string requiredMessage, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            errors[field] = [requiredMessage];
        else if (value.Length > maxLength)
            errors[field] = [$"No puede superar {maxLength} caracteres"];
    }
}