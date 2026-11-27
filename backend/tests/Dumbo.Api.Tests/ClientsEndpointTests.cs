using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dumbo.Api.Tests;

/// <summary>
/// End-to-end HTTP integration tests using real JWTs and in-memory stores covering manual client
/// provisioning, activation, duplicate emails, role authorization, listing, editing, and immediate
/// access revocation on disable (NF02).
/// </summary>
public sealed class ClientsEndpointTests
{
    private const string Password = "Clave1234";

    // Test host wiring in-memory stores with a real API pipeline and JWT bearer auth
    internal sealed class Env : IDisposable
    {
        public FakeAccountLookup Users { get; } = new();
        public InMemoryOutbox Outbox { get; } = new();
        public CapturingEmailSender Sender { get; } = new();
        public InMemoryClientAdminStore Clients { get; }
        public WebApplicationFactory<Program> Factory { get; }
        

        public Env()
        {
            var tokens = new InMemoryTokenStore(Users);
            var accounts = new LookupBackedAccountStore(Users, Outbox);
            Clients = new InMemoryClientAdminStore(Users, Outbox, tokens);

            Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("Email:WorkerEnabled", "false");
                builder.ConfigureServices(s =>
                {
                    s.RemoveAll<IAccountLookup>();
                    s.RemoveAll<IAccountStore>();
                    s.RemoveAll<IClientAdminStore>();
                    s.RemoveAll<IAccountPasswordHasher>();
                    s.RemoveAll<IActivationTokenStore>();
                    s.RemoveAll<IEmailOutboxStore>();
                    s.RemoveAll<IEmailSender>();
                    s.AddSingleton<IAccountLookup>(Users);
                    s.AddSingleton<IAccountStore>(accounts);
                    s.AddSingleton<IClientAdminStore>(Clients);
                    s.AddSingleton<IAccountPasswordHasher>(new PrefixPasswordHasher());
                    s.AddSingleton<IActivationTokenStore>(tokens);
                    s.AddSingleton<IEmailOutboxStore>(Outbox);
                    s.AddSingleton<IEmailSender>(Sender);
                });
            });
        }

        public User AddUser(
            string email, UserRole role, UserStatus status, string? password = Password,
            string firstName = "Test", string lastName = "User", string phone = "+56900000000")
        {
            var user = new User
            {
                Email = email, FirstName = firstName, LastName = lastName, Phone = phone,
                Role = role, Status = status,
                PasswordHash = password is null ? null : "hash:" + password,
                CreatedAt = DateTime.UtcNow
            };
            Users.Users.Add(user);
            return user;
        }

        /// <summary>
        /// Authenticates against the login endpoint and returns an <see cref="HttpClient"/> with its bearer token set
        /// </summary>
        public async Task<HttpClient> SignInAsync(string email)
        {
            var client = Factory.CreateClient();
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString()!);
            return client;
        }

        /// <summary>
        /// Executes a single outbox worker batch on demand
        /// </summary>
        public async Task RunWorkerOnceAsync()
        {
            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ProcessEmailOutboxUseCase>()
                .ExecuteAsync(20, CancellationToken.None);
        }

        public void Dispose() => Factory.Dispose();
    }

    private static object NewClient(string email) =>
        new { email, firstName = "Nueva", lastName = "Cliente", phone = "+56911112222" };

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    // Manual provisioning and activation

    [Fact]
    public async Task Staff_creates_a_pending_client_who_then_activates_the_account_and_signs_in()
    {
        using var env = new Env();
        env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");

        using var created = await staff.PostAsJsonAsync("/api/clients", NewClient("Nueva@Dumbo.cl"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var account = await JsonAsync(created);
        Assert.Equal("nueva@dumbo.cl", account.GetProperty("email").GetString());
        Assert.Equal("Client", account.GetProperty("role").GetString());
        Assert.Equal("PendingActivation", account.GetProperty("status").GetString());

        // Account has no password yet and cannot log in before activation.
        using var anonymous = env.Factory.CreateClient();
        using var early = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "nueva@dumbo.cl", password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, early.StatusCode);

        await env.RunWorkerOnceAsync();
        var message = Assert.Single(env.Sender.Sent);
        Assert.Equal("nueva@dumbo.cl", message.To);
        Assert.Contains("/activar-cuenta?token=", message.TextBody);

        var token = CapturingEmailSender.ExtractToken(message);
        using var activate = await anonymous.PostAsJsonAsync("/api/auth/activate", new { token, password = Password });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "nueva@dumbo.cl", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Resending_the_activation_link_invalidates_the_previous_one()
    {
        using var env = new Env();
        env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");

        using var created = await staff.PostAsJsonAsync("/api/clients", NewClient("nueva@dumbo.cl"));
        var id = (await JsonAsync(created)).GetProperty("id").GetGuid();
        await env.RunWorkerOnceAsync();
        var firstToken = CapturingEmailSender.ExtractToken(env.Sender.Sent[0]);

        using var resend = await staff.PostAsync($"/api/clients/{id}/activation-link", null);
        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        await env.RunWorkerOnceAsync();

        Assert.Equal(2, env.Sender.Sent.Count);
        var secondToken = CapturingEmailSender.ExtractToken(env.Sender.Sent[1]);
        Assert.NotEqual(firstToken, secondToken);

        // Prior token is superseded once the replacement link is issued
        using var anonymous = env.Factory.CreateClient();
        using var old = await anonymous.PostAsJsonAsync("/api/auth/activate", new { token = firstToken, password = Password });
        Assert.Equal(HttpStatusCode.BadRequest, old.StatusCode);
        Assert.Equal("INVALID_OR_EXPIRED_TOKEN", (await JsonAsync(old)).GetProperty("code").GetString());

        using var fresh = await anonymous.PostAsJsonAsync("/api/auth/activate", new { token = secondToken, password = Password });
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);

        using var again = await staff.PostAsync($"/api/clients/{id}/activation-link", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("ACCOUNT_NOT_PENDING", (await JsonAsync(again)).GetProperty("code").GetString());

        using var unknown = await staff.PostAsync($"/api/clients/{Guid.NewGuid()}/activation-link", null);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Creating_a_client_with_a_taken_email_returns_409_even_if_the_account_is_disabled()
    {
        using var env = new Env();
        env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Disabled); // Disabled accounts still reserve the email
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");

        using var response = await staff.PostAsJsonAsync("/api/clients", NewClient("ANA@dumbo.cl"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("EMAIL_ALREADY_REGISTERED", (await JsonAsync(response)).GetProperty("code").GetString());
        Assert.Empty(env.Outbox.Items);
    }

    [Fact]
    public async Task Creating_a_client_with_invalid_data_returns_400_with_errors_per_field()
    {
        using var env = new Env();
        env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");

        using var response = await staff.PostAsJsonAsync("/api/clients",
            new { email = "no-es-correo", firstName = "   ", lastName = "", phone = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await JsonAsync(response);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        var errors = body.GetProperty("errors");
        foreach (var field in new[] { "email", "firstName", "lastName", "phone" })
            Assert.True(errors.TryGetProperty(field, out _), $"Falta el error de '{field}'");
    }

    // Authorization

    [Fact]
    public async Task A_client_or_an_anonymous_caller_cannot_use_the_staff_routes()
    {
        using var env = new Env();
        var target = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Active);
        env.AddUser("cliente@dumbo.cl", UserRole.Client, UserStatus.Active);
        using var client = await env.SignInAsync("cliente@dumbo.cl");
        using var anonymous = env.Factory.CreateClient();

        using var create = await client.PostAsJsonAsync("/api/clients", NewClient("otra@dumbo.cl"));
        using var list = await client.GetAsync("/api/clients");
        using var edit = await client.PutAsJsonAsync($"/api/clients/{target.Id}", NewClient("x@dumbo.cl"));
        using var disable = await client.PostAsync($"/api/clients/{target.Id}/disable", null);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, disable.StatusCode);

        using var noToken = await anonymous.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);

        Assert.DoesNotContain(env.Users.Users, u => u.Email == "otra@dumbo.cl");
        Assert.Equal(UserStatus.Active, target.Status);
    }

    // Listing

    [Fact]
    public async Task List_returns_only_current_clients_with_search_status_filter_and_paging()
    {
        using var env = new Env();
        env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Active, firstName: "Ana", lastName: "Soto");
        env.AddUser("beto@dumbo.cl", UserRole.Client, UserStatus.Disabled, firstName: "Beto", lastName: "Pérez");
        env.AddUser("carla@dumbo.cl", UserRole.Client, UserStatus.PendingActivation, null, "Carla", "Díaz");
        env.AddUser("dario@dumbo.cl", UserRole.Client, UserStatus.Deleted, firstName: "Darío", lastName: "Eliminado");
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");

        var all = await JsonAsync(await staff.GetAsync("/api/clients"));
        Assert.Equal(3, all.GetProperty("total").GetInt32());
        Assert.Equal(1, all.GetProperty("page").GetInt32());
        Assert.Equal(20, all.GetProperty("pageSize").GetInt32());
        var emails = all.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("email").GetString()).ToList();
        Assert.Equal(new[] { "carla@dumbo.cl", "beto@dumbo.cl", "ana@dumbo.cl" }, emails);

        Assert.DoesNotContain("password", all.GetRawText(), StringComparison.OrdinalIgnoreCase);

        var found = await JsonAsync(await staff.GetAsync("/api/clients?search=BETO"));
        Assert.Equal(1, found.GetProperty("total").GetInt32());

        var disabled = await JsonAsync(await staff.GetAsync("/api/clients?status=Disabled"));
        Assert.Equal(1, disabled.GetProperty("total").GetInt32());

        var page2 = await JsonAsync(await staff.GetAsync("/api/clients?page=2&pageSize=2"));
        Assert.Equal(3, page2.GetProperty("total").GetInt32());
        Assert.Equal(1, page2.GetProperty("items").GetArrayLength());

        using var badStatus = await staff.GetAsync("/api/clients?status=Deleted");
        Assert.Equal(HttpStatusCode.BadRequest, badStatus.StatusCode);
    }

    // Editing

    [Fact]
    public async Task Staff_edits_a_client_keeping_its_id_and_rejecting_only_emails_of_other_accounts()
    {
        using var env = new Env();
        env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        var ana = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Active, firstName: "Ana", lastName: "Soto");
        env.AddUser("beto@dumbo.cl", UserRole.Client, UserStatus.Active);
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");
        var url = $"/api/clients/{ana.Id}";

        // Unchanged email is allowed while updating name, last name, and phone.
        using var same = await staff.PutAsJsonAsync(url,
            new { email = "ana@dumbo.cl", firstName = "Ana María", lastName = "Soto Pérez", phone = "+56999999999" });
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        var edited = await JsonAsync(same);
        Assert.Equal("Ana María", edited.GetProperty("firstName").GetString());
        Assert.Equal(ana.Id, edited.GetProperty("id").GetGuid());

        var read = await JsonAsync(await staff.GetAsync(url));
        Assert.Equal("Soto Pérez", read.GetProperty("lastName").GetString());
        Assert.Equal("+56999999999", read.GetProperty("phone").GetString());

        using var taken = await staff.PutAsJsonAsync(url, NewClient("BETO@dumbo.cl"));
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        Assert.Equal("EMAIL_ALREADY_REGISTERED", (await JsonAsync(taken)).GetProperty("code").GetString());
        Assert.Equal("ana@dumbo.cl", ana.Email);

        using var invalid = await staff.PutAsJsonAsync(url,
            new { email = "ana@dumbo.cl", firstName = " ", lastName = "Soto", phone = "+56900000000" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var missing = await staff.PutAsJsonAsync($"/api/clients/{Guid.NewGuid()}", NewClient("z@dumbo.cl"));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        using var change = await staff.PutAsJsonAsync(url, NewClient("Ana.Soto@Dumbo.cl"));
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        using var anonymous = env.Factory.CreateClient();
        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "ana.soto@dumbo.cl", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    // Disable: immediate access revocation

    [Fact]
    public async Task Reenabling_a_client_requires_a_new_session()
    {
        using var env = new Env();
        env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        var ana = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Active);
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");
        using var session = await env.SignInAsync("ana@dumbo.cl");
        using var anonymous = env.Factory.CreateClient();

        using var before = await session.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        using var disable = await staff.PostAsync($"/api/clients/{ana.Id}/disable", null);
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        Assert.Equal("Disabled", (await JsonAsync(disable)).GetProperty("status").GetString());

        // Existing non-expired JWT is rejected immediately on subsequent requests.
        using var after = await session.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);

        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "ana@dumbo.cl", password = Password });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Equal("Usuario deshabilitado", (await JsonAsync(login)).GetProperty("title").GetString());

        using var again = await staff.PostAsync($"/api/clients/{ana.Id}/disable", null);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        using var enable = await staff.PostAsync($"/api/clients/{ana.Id}/enable", null);
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
        Assert.Equal("Active", (await JsonAsync(enable)).GetProperty("status").GetString());
        using var oldSession = await session.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
        using var relogin = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "ana@dumbo.cl", password = Password });
        Assert.Equal(HttpStatusCode.OK, relogin.StatusCode);

        using var unknown = await staff.PostAsync($"/api/clients/{Guid.NewGuid()}/disable", null);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
