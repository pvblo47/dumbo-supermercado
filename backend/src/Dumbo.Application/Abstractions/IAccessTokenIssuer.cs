using Dumbo.Domain.Entities;

namespace Dumbo.Application.Abstractions;

/// <summary>Signed access token and the UTC timestamp when it expires.</summary>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

/// <summary>
/// Issues access tokens for an authenticated account. Application only depends on this
/// interface; whether the token is a signed JWT is decided by Infrastructure.
/// </summary>
public interface IAccessTokenIssuer
{
    /// <summary>Issues an access token for the specified authenticated user.</summary>
    AccessToken Issue(User user);
}