using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Dumbo.Infrastructure.Security;

/// <summary>
/// PBKDF2 password hasher implementation utilizing ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/>,
/// providing versioned cryptographic hashing and secure verification.
/// </summary>
internal sealed class IdentityAccountPasswordHasher : IAccountPasswordHasher
{
    private static readonly User NoUser = new();   // el hasher no usa el usuario
    private readonly PasswordHasher<User> hasher = new();

    /// <inheritdoc />
    public string Hash(string password) => hasher.HashPassword(NoUser, password);

    /// <inheritdoc />
    public bool Verify(string passwordHash, string password) =>
        hasher.VerifyHashedPassword(NoUser, passwordHash, password) != PasswordVerificationResult.Failed;
}