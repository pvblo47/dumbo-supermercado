namespace Dumbo.Application.Abstractions;

/// <summary>
/// Defines password hashing and verification contracts for user accounts,
/// decoupling cryptographic implementation details from the application layer.
/// </summary>
public interface IAccountPasswordHasher
{
    /// <summary>
    /// Generates a secure cryptographic hash from a plain-text password.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifies whether a plain-text password matches a stored cryptographic hash.
    /// </summary>
    bool Verify(string passwordHash, string password);
}