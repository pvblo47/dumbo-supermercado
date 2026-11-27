namespace Dumbo.Api.Contracts;

/// <summary>
/// Request payload for <c>POST /api/clients</c> (manual creation) and <c>PUT /api/clients/{id}</c> (update).
/// Nullable properties are intentional so validation and error messages stay in the Application layer;
/// password, role, and RUT are excluded, and extra properties are ignored.
/// </summary>
public sealed record ClientDataRequest(string? Email, string? FirstName, string? LastName, string? Phone);