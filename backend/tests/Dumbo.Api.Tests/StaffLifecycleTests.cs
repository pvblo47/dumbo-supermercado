using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dumbo.Application.Clients;
using Dumbo.Domain.Enums;

namespace Dumbo.Api.Tests;

/// <summary>
/// Integration tests covering worker management permissions and account soft-deletion
/// and restoration flows across roles, including access revocation, audit entries, and idempotency (D07).
/// </summary>
public sealed class StaffLifecycleTests
{
    private const string Password = "Clave1234";

    private static object Data(string email) =>
        new { email, firstName = "Nuevo", lastName = "Trabajador", phone = "+56911112222" };

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage r) =>
        await r.Content.ReadFromJsonAsync<JsonElement>();

    // Workers

    [Fact]
    public async Task Admin_creates_a_worker_who_activates_and_works_with_clients_but_cannot_manage_workers()
    {
        using var env = new ClientsEndpointTests.Env();
        env.AddUser("admin@dumbo.cl", UserRole.Admin, UserStatus.Active);
        using var admin = await env.SignInAsync("admin@dumbo.cl");

        using var created = await admin.PostAsJsonAsync("/api/workers", Data("Trab@Dumbo.cl"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var account = await JsonAsync(created);
        Assert.Equal("Worker", account.GetProperty("role").GetString());
        Assert.Equal("PendingActivation", account.GetProperty("status").GetString());

        await env.RunWorkerOnceAsync();
        var token = CapturingEmailSender.ExtractToken(Assert.Single(env.Sender.Sent));
        using var anonymous = env.Factory.CreateClient();
        using var activate = await anonymous.PostAsJsonAsync("/api/auth/activate", new { token, password = Password });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        using var worker = await env.SignInAsync("trab@dumbo.cl");
        using var clients = await worker.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.OK, clients.StatusCode);

        using var list = await worker.GetAsync("/api/workers");
        using var create = await worker.PostAsJsonAsync("/api/workers", Data("otro@dumbo.cl"));
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Each_route_only_sees_its_own_role_and_administrators_are_out_of_reach()
    {
        using var env = new ClientsEndpointTests.Env();
        var admin = env.AddUser("admin@dumbo.cl", UserRole.Admin, UserStatus.Active);
        var worker = env.AddUser("trab@dumbo.cl", UserRole.Worker, UserStatus.Active);
        var client = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Active);
        using var session = await env.SignInAsync("admin@dumbo.cl");

        var workers = await JsonAsync(await session.GetAsync("/api/workers"));
        Assert.Equal(1, workers.GetProperty("total").GetInt32());
        var clients = await JsonAsync(await session.GetAsync("/api/clients"));
        Assert.Equal(1, clients.GetProperty("total").GetInt32());

        // Cross-role lookups or targeting an Admin return 404 without mutating state.
        using var workerAsClient = await session.GetAsync($"/api/clients/{worker.Id}");
        using var clientAsWorker = await session.PostAsync($"/api/workers/{client.Id}/disable", null);
        using var adminAsWorker = await session.DeleteAsync($"/api/workers/{admin.Id}");
        Assert.Equal(HttpStatusCode.NotFound, workerAsClient.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, clientAsWorker.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminAsWorker.StatusCode);
        Assert.Equal("WORKER_NOT_FOUND", (await JsonAsync(adminAsWorker)).GetProperty("code").GetString());
        Assert.Equal(UserStatus.Active, client.Status);
        Assert.Equal(UserStatus.Active, admin.Status);
    }

    // Soft Delete

    [Fact]
    public async Task Deleting_a_client_removes_access_and_listing_keeps_the_email_reserved_and_is_audited_once()
    {
        using var env = new ClientsEndpointTests.Env();
        var staffUser = env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        var ana = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Active);
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");
        using var session = await env.SignInAsync("ana@dumbo.cl");
        using var anonymous = env.Factory.CreateClient();

        using var delete = await staff.DeleteAsync($"/api/clients/{ana.Id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal("Deleted", (await JsonAsync(delete)).GetProperty("status").GetString());
        Assert.NotNull(ana.DeletedAt);

        // Removed from active list, shown in deleted view, and active sessions/logins are rejected.
        var current = await JsonAsync(await staff.GetAsync("/api/clients"));
        Assert.Equal(0, current.GetProperty("total").GetInt32());
        var deleted = await JsonAsync(await staff.GetAsync("/api/clients/deleted"));
        Assert.Equal(1, deleted.GetProperty("total").GetInt32());
        using var me = await session.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "ana@dumbo.cl", password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        // Email remains reserved (D07.6) and updates cannot target soft-deleted accounts.
        using var again = await staff.PostAsJsonAsync("/api/clients",
            new { email = "ana@dumbo.cl", firstName = "Otra", lastName = "Ana", phone = "+56900000000" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        using var edit = await staff.PutAsJsonAsync($"/api/clients/{ana.Id}",
            new { email = "ana@dumbo.cl", firstName = "X", lastName = "Y", phone = "+56900000000" });
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);

        // Idempotent delete: repeating returns 200 and records no duplicate audit entry.
        using var repeat = await staff.DeleteAsync($"/api/clients/{ana.Id}");
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        var entry = Assert.Single(env.Clients.Audit);
        Assert.Equal(AccountAuditAction.Deleted, entry.Action);
        Assert.Equal(ana.Id, entry.TargetUserId);
        Assert.Equal(staffUser.Id, entry.ActorUserId);
        Assert.Equal(UserStatus.Active, entry.PreviousStatus);
        Assert.Equal(UserStatus.Deleted, entry.NewStatus);
    }

    [Fact]
    public async Task Restoring_returns_the_same_account_as_disabled_and_it_must_be_enabled_afterwards()
    {
        using var env = new ClientsEndpointTests.Env();
        env.AddUser("trabajador@dumbo.cl", UserRole.Worker, UserStatus.Active);
        var ana = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Active);
        using var staff = await env.SignInAsync("trabajador@dumbo.cl");
        using var anonymous = env.Factory.CreateClient();

        // Restoring a non-deleted account returns 409 Conflict.
        using var notDeleted = await staff.PostAsync($"/api/clients/{ana.Id}/restore", null);
        Assert.Equal(HttpStatusCode.Conflict, notDeleted.StatusCode);
        Assert.Equal("ACCOUNT_NOT_DELETED", (await JsonAsync(notDeleted)).GetProperty("code").GetString());

        await staff.DeleteAsync($"/api/clients/{ana.Id}");
        using var restore = await staff.PostAsync($"/api/clients/{ana.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        var restored = await JsonAsync(restore);
        Assert.Equal("Disabled", restored.GetProperty("status").GetString());
        Assert.Equal(ana.Id, restored.GetProperty("id").GetGuid());
        Assert.Null(ana.DeletedAt);

        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "ana@dumbo.cl", password = Password });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);

        using var enable = await staff.PostAsync($"/api/clients/{ana.Id}/enable", null);
        Assert.Equal("Active", (await JsonAsync(enable)).GetProperty("status").GetString());
        using var relogin = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "ana@dumbo.cl", password = Password });
        Assert.Equal(HttpStatusCode.OK, relogin.StatusCode);

        Assert.Equal(new[] { AccountAuditAction.Deleted, AccountAuditAction.Restored },
            env.Clients.Audit.Select(a => a.Action).ToArray());
    }

    [Fact]
    public async Task Admin_deletes_and_restores_a_worker_but_a_worker_cannot()
    {
        using var env = new ClientsEndpointTests.Env();
        env.AddUser("admin@dumbo.cl", UserRole.Admin, UserStatus.Active);
        var worker = env.AddUser("trab@dumbo.cl", UserRole.Worker, UserStatus.Active);
        using var admin = await env.SignInAsync("admin@dumbo.cl");
        using var workerSession = await env.SignInAsync("trab@dumbo.cl");

        using var denied = await workerSession.DeleteAsync($"/api/workers/{worker.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(UserStatus.Active, worker.Status);

        using var delete = await admin.DeleteAsync($"/api/workers/{worker.Id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        using var after = await workerSession.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);

        using var restore = await admin.PostAsync($"/api/workers/{worker.Id}/restore", null);
        Assert.Equal("Disabled", (await JsonAsync(restore)).GetProperty("status").GetString());
    }
}