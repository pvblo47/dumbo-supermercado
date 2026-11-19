using Dumbo.Domain.Entities;

namespace Dumbo.Application.Accounts;

/// <summary>Converts Domain entities into DTOs returned by the API.</summary>
public static class AccountMappings
{
    /// <summary>
    /// Public account data. Never includes the password hash, and there is no RUT
    /// The API does not expose EF entities directly.
    /// </summary>
    public static AccountSummary ToSummary(this User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Phone,
            user.Role.ToString(), user.Status.ToString());
}