using System.Text;
using Dumbo.Application.Abstractions;
using Dumbo.Application.Security;
using Dumbo.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Dumbo.Infrastructure.Security;

/// <summary>
/// Implements <see cref="IAccessTokenIssuer"/> by issuing an HMAC-SHA256 signed JWT.
/// The token carries the account identifier, email, and role, but NO sensitive data: a JWT
/// can be decoded (it is not secret), it is only protected against tampering.
/// </summary>
internal sealed class JwtAccessTokenIssuer(JwtSettings settings, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler handler = new();

    /// <summary>Issues a signed JWT access token for the specified user.</summary>
    public AccessToken Issue(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(settings.ExpiresMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,

            Claims = new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["email"] = user.Email,
                ["role"] = user.Role.ToString(),
                [JwtSettings.SessionVersionClaim] = user.SessionVersion.ToString(),
                ["jti"] = Guid.NewGuid().ToString()
            },

            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(handler.CreateToken(descriptor), expires);
    }
}
