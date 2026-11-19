namespace Dumbo.Api.Contracts;

/// <summary>
/// Request body for POST /api/auth/login. Properties are intentionally nullable:
/// validation and error messages live in Application. No role or RUT fields are included.
/// </summary>
public sealed record LoginRequest(string? Email, string? Password);