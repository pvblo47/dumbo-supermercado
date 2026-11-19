using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;

namespace Dumbo.Api.Tests;

/// <summary>
/// In-memory account query store replacing <c>AccountLookup</c> (which uses PostgreSQL).
/// Tests populate <see cref="Users"/> and can mutate account state to simulate an administrator
/// disabling an account mid-session.
/// </summary>
internal sealed class FakeAccountLookup : IAccountLookup
{
    public List<User> Users { get; } = [];

    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == normalizedEmail));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
}

/// <summary>
/// Test password hasher that distinguishes passwords (unlike the registration fake hasher):
/// each "hash" is "hash:" followed by the password, allowing both valid and invalid password tests.
/// </summary>
internal sealed class PrefixPasswordHasher : IAccountPasswordHasher
{
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string passwordHash, string password) => passwordHash == "hash:" + password;
}

/// <summary>Fake token issuer returning a deterministic token string without cryptographic signing.</summary>
internal sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public AccessToken Issue(User user) =>
        new("token-for-" + user.Email, new DateTime(2026, 11, 18, 13, 0, 0, DateTimeKind.Utc));
}