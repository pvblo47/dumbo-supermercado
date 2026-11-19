namespace Dumbo.Application.Security;

/// <summary>
/// Parameters used to sign and validate JWT tokens.
/// Resolved once at startup (see JwtSettingsFactory in Infrastructure) and shared
/// between token issuance and API validation so they always stay in sync.
/// </summary>
/// <param name="Issuer">Entity that issues the token (the API).</param>
/// <param name="Audience">Intended recipient of the token (the Angular application).</param>
/// <param name="SigningKey">Secret signing key. Never logged or committed to Git.</param>
/// <param name="ExpiresMinutes">Token lifetime in minutes from issuance.</param>
public sealed record JwtSettings(string Issuer, string Audience, string SigningKey, int ExpiresMinutes);