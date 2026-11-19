using System.Security.Cryptography;
using System.Text;

namespace Dumbo.Application.Accounts;

/// <summary>
/// Generates and hashes single-use link tokens. Because each raw token contains 256 random bits,
/// an unsalted SHA-256 digest is sufficient for storage and cannot be guessed or cracked via dictionary attacks.
/// </summary>
public static class SecureToken
{
    /// <summary>
    /// Generates a new token pair: <c>Raw</c> is embedded in the email link, while <c>Hash</c> is persisted in storage.
    /// </summary>
    public static (string Raw, string Hash) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        // URL-safe Base64 encoding without '+', '/', or '=' characters.
        var raw = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (raw, Hash(raw));
    }

    /// <summary>
    /// Computes the lowercase 64-character hexadecimal SHA-256 digest of the raw token.
    /// </summary>
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}