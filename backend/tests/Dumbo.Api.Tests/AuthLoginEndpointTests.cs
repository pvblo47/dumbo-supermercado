using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dumbo.Application.Abstractions;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dumbo.Api.Tests;

/// <summary>
/// End-to-end authentication tests: sign-in, real JWT issuance, account revalidation (NF02),
/// and role authorization (401 / 403). The database is replaced with an in-memory lookup, while
/// JWT issuance and validation run for real using the ephemeral key generated in Development.
/// </summary>
public sealed class AuthLoginEndpointTests
{
    private const string Password = "Segura1234";

    private static WebApplicationFactory<Program> CreateFactory(FakeAccountLookup lookup) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");   // allows the ephemeral JWT signing key
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAccountLookup>();
                services.AddSingleton<IAccountLookup>(lookup);
                services.RemoveAll<IAccountPasswordHasher>();
                services.AddSingleton<IAccountPasswordHasher, PrefixPasswordHasher>();
            });
        });

    private static User MakeUser(string email, UserRole role, UserStatus status = UserStatus.Active) => new()
    {
        Email = email,
        FirstName = "Ana",
        LastName = "Pérez",
        Phone = "+56912345678",
        PasswordHash = "hash:" + Password,
        Role = role,
        Status = status,
        CreatedAt = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc)
    };

    // Signs in and returns the bearer token; fails the test if login does not return 200 OK.
    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private static void UseToken(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Login_returns_a_bearer_token_that_opens_the_me_endpoint()
    {
        var lookup = new FakeAccountLookup();
        lookup.Users.Add(MakeUser("ana@mail.com", UserRole.Client));
        await using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();

        using var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "ana@mail.com", password = Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bearer", session.GetProperty("tokenType").GetString());
        Assert.Equal("Client", session.GetProperty("account").GetProperty("role").GetString());
        Assert.DoesNotContain("password", await login.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);

        UseToken(client, session.GetProperty("accessToken").GetString()!);
        using var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var account = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ana@mail.com", account.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_with_the_ers_message()
    {
        var lookup = new FakeAccountLookup();
        lookup.Users.Add(MakeUser("ana@mail.com", UserRole.Client));
        await using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "ana@mail.com", password = "OtraClave123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_CREDENTIALS", body.GetProperty("code").GetString());
        Assert.Equal(
            "Las credenciales de acceso son incorrectas o el usuario no está registrado en el sistema",
            body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Login_of_a_disabled_account_returns_403_with_the_disabled_message()
    {
        var lookup = new FakeAccountLookup();
        lookup.Users.Add(MakeUser("ana@mail.com", UserRole.Client, UserStatus.Disabled));
        await using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "ana@mail.com", password = Password });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ACCOUNT_DISABLED", body.GetProperty("code").GetString());
        Assert.Equal("Usuario deshabilitado", body.GetProperty("title").GetString());
    }

    // NF02: the account is disabled AFTER sign-in. The token is still unexpired and validly signed,
    // but the API must still reject it because the account is no longer Active in backing storage.
    [Fact]
    public async Task A_token_stops_working_when_the_account_is_disabled_after_login()
    {
        var lookup = new FakeAccountLookup();
        var user = MakeUser("ana@mail.com", UserRole.Client);
        lookup.Users.Add(user);
        await using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();

        UseToken(client, await LoginAsync(client, "ana@mail.com"));
        using var before = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        user.Status = UserStatus.Disabled;  

        using var after = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task Me_without_a_token_returns_401()
    {
        await using var factory = CreateFactory(new FakeAccountLookup());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Role authorization is enforced on the backend: 401 without a session, 403 when lacking role.
    [Fact]
    public async Task Staff_endpoint_enforces_roles_in_the_backend()
    {
        var lookup = new FakeAccountLookup();
        lookup.Users.Add(MakeUser("cliente@mail.com", UserRole.Client));
        lookup.Users.Add(MakeUser("trabajador@mail.com", UserRole.Worker));
        await using var factory = CreateFactory(lookup);

        using var anonymous = factory.CreateClient();
        using var noToken = await anonymous.GetAsync("/api/auth/staff-check");
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);

        using var clientUser = factory.CreateClient();
        UseToken(clientUser, await LoginAsync(clientUser, "cliente@mail.com"));
        using var asClient = await clientUser.GetAsync("/api/auth/staff-check");
        Assert.Equal(HttpStatusCode.Forbidden, asClient.StatusCode);

        using var workerUser = factory.CreateClient();
        UseToken(workerUser, await LoginAsync(workerUser, "trabajador@mail.com"));
        using var asWorker = await workerUser.GetAsync("/api/auth/staff-check");
        Assert.Equal(HttpStatusCode.OK, asWorker.StatusCode);
    }
}