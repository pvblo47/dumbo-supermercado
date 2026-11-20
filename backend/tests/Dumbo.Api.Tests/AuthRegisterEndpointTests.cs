using System.Net;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Json;
using System.Text.Json;
using Dumbo.Application.Abstractions;
using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dumbo.Api.Tests;

/// <summary>
/// End-to-end tests for the <c>POST /api/auth/register</c> endpoint using an in-memory
/// <see cref="WebApplicationFactory{TEntryPoint}"/> with fake store and password hasher
/// implementations, avoiding external database dependencies.
/// </summary>
public sealed class AuthRegisterEndpointTests
{
    // Spins up the full API in memory while replacing the real store and hasher with test fakes.
    private static WebApplicationFactory<Program> CreateFactory(FakeAccountStore store) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Email:WorkerEnabled", "false");  
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAccountStore>();
                services.AddSingleton<IAccountStore>(store);
                services.RemoveAll<IAccountPasswordHasher>();
                services.AddSingleton<IAccountPasswordHasher, FakePasswordHasher>();
            });
        });

    // NF02 and RF-002: self-registration only creates Clients; any requested role in the payload
    // must be ignored so privilege escalation cannot occur.
    /// <summary>
    /// Verifies that registration creates a Client account (201 Created), ignores an attempted
    /// privilege escalation role in the request body, and omits password data from the response.
    /// </summary>
    [Fact]
    public async Task Register_creates_a_client_even_if_the_body_asks_for_another_role()
    {
        var store = new FakeAccountStore();
        await using var factory = CreateFactory(store);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "ana@mail.com", firstName = "Ana", lastName = "Pérez",
            phone = "+56912345678", password = "Segura1234", role = "Admin"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);   // 201
        Assert.Equal(UserRole.Client, Assert.Single(store.Added).Role);

        // Response payload must never expose the password or its hash.
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that a weak password returns 400 Bad Request with RFC 7807 problem+json
    /// formatting, a <c>VALIDATION_ERROR</c> code, and the expected field error message.
    /// </summary>
    [Fact]
    public async Task Register_returns_400_with_field_errors_for_a_weak_password()
    {
        await using var factory = CreateFactory(new FakeAccountStore());
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "ana@mail.com", firstName = "Ana", lastName = "Pérez",
            phone = "+56912345678", password = "debil"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);   // 400
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.Equal(
            "La nueva contraseña no cumple los estándares de seguridad",
            body.GetProperty("errors").GetProperty("password")[0].GetString());
    }

    /// <summary>
    /// Verifies that registering an email already present in the store (even with different casing)
    /// returns 409 Conflict with <c>EMAIL_ALREADY_REGISTERED</c> and does not persist a duplicate.
    /// </summary>
    [Fact]
    public async Task Register_returns_409_when_the_email_is_taken()
    {
        var store = new FakeAccountStore();
        store.ExistingEmails.Add("ana@mail.com");
        await using var factory = CreateFactory(store);
        using var client = factory.CreateClient();

        // Sent as "ANA@mail.com": must still be detected despite casing differences.
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "ANA@mail.com", firstName = "Ana", lastName = "Pérez",
            phone = "+56912345678", password = "Segura1234"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);   // 409
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMAIL_ALREADY_REGISTERED", body.GetProperty("code").GetString());
        Assert.Empty(store.Added);
    }
}