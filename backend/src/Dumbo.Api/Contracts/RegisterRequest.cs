namespace Dumbo.Api.Contracts;

/// <summary>
/// API request payload for client self-registration.
/// Properties are intentionally nullable: validation logic and error messages reside in the Application layer.
/// Excludes role and RUT fields; any extra JSON properties are automatically ignored.
/// </summary>
public sealed record RegisterRequest(
    string? Email, 
    string? FirstName, 
    string? LastName, 
    string? Phone, 
    string? Password);