using System.Security.Cryptography;
using System.Text;
using Dumbo.Application.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Dumbo.Infrastructure.Security;

/// <summary>
/// Builds <see cref="JwtSettings"/> from configuration (the "Jwt" section).
/// Issuer, Audience, and ExpiresMinutes have defaults; the signing key does not.
/// </summary>
internal static class JwtSettingsFactory
{
    private const int MinKeyBytes = 32;

    /// <summary>
    /// Creates and validates <see cref="JwtSettings"/> for the current host environment.
    /// </summary>
    public static JwtSettings Create(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Jwt");
        var issuer = section["Issuer"] ?? "dumbo-api";
        var audience = section["Audience"] ?? "dumbo-web";
        var expiresMinutes = int.TryParse(section["ExpiresMinutes"], out var minutes) && minutes > 0 ? minutes : 60;

        var key = section["Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException(
                    "Falta la configuración 'Jwt:Key'. Defínela como variable de entorno Jwt__Key (mínimo 32 caracteres).");

            key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        }

        if (Encoding.UTF8.GetByteCount(key) < MinKeyBytes)
            throw new InvalidOperationException("'Jwt:Key' debe tener al menos 32 caracteres.");

        return new JwtSettings(issuer, audience, key, expiresMinutes);
    }
}