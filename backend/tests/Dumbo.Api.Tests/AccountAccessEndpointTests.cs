using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Dumbo.Api.Tests;

public class AccountAccessEndpointTests
{
    private sealed class Env
    {
        public FakeAccountLookup Lookup { get; } = new();
        public InMemoryOutbox Outbox { get; } = new();
        public CapturingEmailSender Sender { get; } = new();
        public WebApplicationFactory<Program> Factory { get; }
        public HttpClient Client { get; }

        public Env()
        {
            var tokens = new InMemoryTokenStore(Lookup);
            Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("Email:WorkerEnabled", "false"); 
                builder.ConfigureServices(s =>
                {
                    s.RemoveAll<IAccountLookup>();
                    s.RemoveAll<IAccountPasswordHasher>();
                    s.RemoveAll<IActivationTokenStore>();
                    s.RemoveAll<IEmailOutboxStore>();
                    s.RemoveAll<IEmailSender>();
                    s.AddSingleton<IAccountLookup>(Lookup);
                    s.AddSingleton<IAccountPasswordHasher>(new PrefixPasswordHasher());
                    s.AddSingleton<IActivationTokenStore>(tokens);
                    s.AddSingleton<IEmailOutboxStore>(Outbox);
                    s.AddSingleton<IEmailSender>(Sender);
                });
            });
            Client = Factory.CreateClient();
        }

        public User AddUser(string email, UserStatus status, string? password = "Clave1234")
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Client,
                Status = status,
                PasswordHash = password is null ? null : "hash:" + password,
                CreatedAt = DateTime.UtcNow,
            };
            Lookup.Users.Add(user);
            return user;
        }

        public async Task RunWorkerOnceAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessEmailOutboxUseCase>();
            await useCase.ExecuteAsync(20, CancellationToken.None);
        }

        public Task<HttpResponseMessage> PostAsync(string url, object body) =>
            Client.PostAsJsonAsync(url, body);

        public async Task<string> ErrorCodeAsync(HttpResponseMessage r)
        {
            using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("code").GetString()!;
        }
    }

    [Theory]
    [InlineData("nadie@dumbo.cl")]  
    [InlineData("activo@dumbo.cl")]  
    public async Task Forgot_always_answers_202_so_it_does_not_reveal_which_emails_exist(string email)
    {
        var env = new Env();
        env.AddUser("activo@dumbo.cl", UserStatus.Active);

        var r = await env.PostAsync("api/auth/password/forgot", new { email });

        Assert.Equal(HttpStatusCode.Accepted, r.StatusCode);
    }

    [Fact]
    public async Task Full_reset_flow_forgot_email_reset_then_login_with_new_password()
    {
        var env = new Env();
        env.AddUser("ana@dumbo.cl", UserStatus.Active, "ClaveVieja1");

        Assert.Equal(HttpStatusCode.Accepted,
            (await env.PostAsync("api/auth/password/forgot", new { email = "ana@dumbo.cl" })).StatusCode);

        await env.RunWorkerOnceAsync();                     
        var token = CapturingEmailSender.ExtractToken(env.Sender.Sent.Single());

        var reset = await env.PostAsync("api/auth/password/reset",
            new { token, password = "ClaveNueva9" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var ok = await env.PostAsync("api/auth/login", new { email = "ana@dumbo.cl", password = "ClaveNueva9" });
        var old = await env.PostAsync("api/auth/login", new { email = "ana@dumbo.cl", password = "ClaveVieja1" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, old.StatusCode);

        var again = await env.PostAsync("api/auth/password/reset", new { token, password = "OtraClave99" });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal("INVALID_OR_EXPIRED_TOKEN", await env.ErrorCodeAsync(again));
    }

    [Fact]
    public async Task Activation_flow_lets_a_pending_account_set_its_password_and_log_in()
    {
        var env = new Env();
        env.AddUser("nuevo@dumbo.cl", UserStatus.PendingActivation, password: null);

        await env.PostAsync("api/auth/password/forgot", new { email = "nuevo@dumbo.cl" });
        await env.RunWorkerOnceAsync();
        var token = CapturingEmailSender.ExtractToken(env.Sender.Sent.Single());

        var activate = await env.PostAsync("api/auth/activate", new { token, password = "Clave1234" });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var login = await env.PostAsync("api/auth/login", new { email = "nuevo@dumbo.cl", password = "Clave1234" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Weak_password_is_rejected_and_does_not_burn_the_link()
    {
        var env = new Env();
        env.AddUser("ana@dumbo.cl", UserStatus.Active);
        await env.PostAsync("api/auth/password/forgot", new { email = "ana@dumbo.cl" });
        await env.RunWorkerOnceAsync();
        var token = CapturingEmailSender.ExtractToken(env.Sender.Sent.Single());

        var weak = await env.PostAsync("api/auth/password/reset", new { token, password = "corta" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await env.ErrorCodeAsync(weak));
        var good = await env.PostAsync("api/auth/password/reset", new { token, password = "ClaveNueva9" });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
    }

    [Fact]
    public async Task Unknown_token_answers_400_INVALID_OR_EXPIRED_TOKEN()
    {
        var env = new Env();

        var r = await env.PostAsync("api/auth/activate", new { token = "no-existe", password = "Clave1234" });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("INVALID_OR_EXPIRED_TOKEN", await env.ErrorCodeAsync(r));
    }

    [Fact]
    public async Task Disabled_accounts_get_no_email()
    {
        var env = new Env();
        env.AddUser("baja@dumbo.cl", UserStatus.Disabled);

        await env.PostAsync("api/auth/password/forgot", new { email = "baja@dumbo.cl" });
        await env.RunWorkerOnceAsync();

        Assert.Empty(env.Sender.Sent);
    }
}