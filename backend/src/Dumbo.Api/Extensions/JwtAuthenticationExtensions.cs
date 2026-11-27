using System.Text;
using Dumbo.Application.Abstractions;
using Dumbo.Application.Security;
using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Dumbo.Api.Extensions;

/// <summary>Configures how the API validates incoming JWT tokens from the Authorization header.</summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>Registers JWT bearer authentication and authorization services for the API.</summary>
    public static IServiceCollection AddDumboJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Options are populated from JwtSettings (using the same signing key as the token issuer).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, settings) =>
            {
                // Keep short token claim names ("sub", "role") without remapping to long claim URIs.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    // Read the principal name from "sub" and roles from "role" for [Authorize(Roles = "...")].
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = RevalidateAccountAsync
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// NF02: a valid signature alone is not enough. On every request, the account is queried in the
    /// database and rejected if it no longer exists, is not Active, or changed role. Disabling or
    /// deleting an account therefore revokes access immediately without waiting for token expiration.
    /// </summary>
    private static async Task RevalidateAccountAsync(TokenValidatedContext context)
    {
        var idText = context.Principal?.FindFirst("sub")?.Value;
        var roleInToken = context.Principal?.FindFirst("role")?.Value;
        var versionInToken = context.Principal?.FindFirst(JwtSettings.SessionVersionClaim)?.Value;

        if (!Guid.TryParse(idText, out var accountId) || !Guid.TryParse(versionInToken, out var sessionVersion))
        {
            context.Fail("El token no contiene una sesión válida.");
            return;
        }

        var lookup = context.HttpContext.RequestServices.GetRequiredService<IAccountLookup>();
        var user = await lookup.FindByIdAsync(accountId, context.HttpContext.RequestAborted);

        if (user is null || user.Status != UserStatus.Active || user.Role.ToString() != roleInToken ||
            user.SessionVersion != sessionVersion)
            context.Fail("La sesión ya no está vigente.");
    }
}
