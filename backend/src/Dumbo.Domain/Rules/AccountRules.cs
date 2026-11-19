using System.Net.Mail;

namespace Dumbo.Domain.Rules;

/// <summary>
/// Encapsulates business validation rules and domain logic for user accounts, 
/// enforcing security policies and formatting standards
/// </summary>
public static class AccountRules
{
    /// <summary>
    /// Literal error message
    /// </summary>
    public const string WeakPasswordMessage = "La nueva contraseña no cumple los estándares de seguridad";

    public const int MinPasswordLength = 9;
    public const int MaxPasswordLength = 128;   // Technical limit to prevent huge input hashes
    public const int MaxEmailLength = 255;

    /// <summary>
    /// Normalizes an email by trimming whitespace and converting it to lowercase
    /// </summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();


    /// <summary>
    /// Validates whether a normalized email meets length, format, and domain criteria.
    /// Rejects display names and domains lacking a dot.
    /// </summary>
    public static bool IsValidEmail(string normalizedEmail)
    {
        if (normalizedEmail.Length is 0 or > MaxEmailLength)
            return false;
        if (!MailAddress.TryCreate(normalizedEmail, out var parsed))
            return false;

        return parsed.Address == normalizedEmail && parsed.Host.Contains('.');
    }

    /// <summary>
    /// Validates password compliance: minimum 9 characters, maximum 128, 
    /// requiring at least one uppercase letter, one lowercase letter, and one digit (RF-001/RF-002).
    /// </summary>
    public static bool IsValidPassword(string? password)
    {
        if (password is null || password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
            return false;

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (c is >= '0' and <= '9') hasDigit = true;
        }
        return hasUpper && hasLower && hasDigit;
    }
}