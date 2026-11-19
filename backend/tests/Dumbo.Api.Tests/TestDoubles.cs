using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;

namespace Dumbo.Api.Tests;

// Test fakes: lightweight in-memory implementations of Application abstractions used to
// test registration flows without PostgreSQL, real password hashing, or system clock drift.
// Marked internal so they remain scoped to the test assembly.

/// <summary>
/// In-memory fake implementation of <see cref="IAccountStore"/> for simulating pre-existing
/// accounts, race conditions during concurrent inserts, and captured user entities.
/// </summary>
internal sealed class FakeAccountStore : IAccountStore
{
    /// <summary>
    /// Gets the set of normalized email addresses treated as already registered
    /// </summary>
    public HashSet<string> ExistingEmails { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="TryAddAsync"/> should fail to simulate
    /// a concurrent registration winning the unique constraint check (mapped to <c>uq_users_email</c> in production).
    /// </summary>
    public bool LoseRaceOnAdd { get; set; }

    /// <summary>
    /// Gets the list of <see cref="User"/> instances passed to <see cref="TryAddAsync"/> and accepted
    /// </summary>
    public List<User> Added { get; } = [];

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingEmails.Contains(normalizedEmail));

    public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        if (LoseRaceOnAdd)
            return Task.FromResult(false);

        Added.Add(user);
        return Task.FromResult(true);
    }
}

/// <summary>
/// Deterministic fake implementation of <see cref="IAccountPasswordHasher"/> that returns a fixed
/// placeholder hash so tests stay fast and can assert the raw password is never stored.
/// </summary>
internal sealed class FakePasswordHasher : IAccountPasswordHasher
{
    public string Hash(string password) => "hashed-value";

    public bool Verify(string passwordHash, string password) => passwordHash == "hashed-value";
}

/// <summary>
/// Deterministic <see cref="TimeProvider"/> returning a fixed UTC timestamp so time-dependent fields
/// such as <c>CreatedAt</c> can be asserted predictably.
/// </summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}