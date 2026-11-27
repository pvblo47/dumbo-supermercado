using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Api.Tests;

/// <summary>
/// Integration tests verifying administrator listing and manual requeuing of exhausted outbox
/// emails, role authorization boundaries, and worker state re-validation prior to sending.
/// </summary>
public sealed class AdminEmailRetryTests
{
    private static async Task<JsonElement> JsonAsync(HttpResponseMessage r) =>
        await r.Content.ReadFromJsonAsync<JsonElement>();

    // Creates a failed outbox item after exhausting all 4 delivery attempts
    private static EmailOutboxItem FailedItem(User user) => new()
    {
        UserId = user.Id, ToEmail = user.Email, Kind = ActivationTokenType.AccountActivation,
        Status = EmailOutboxStatus.Failed, AttemptCount = EmailOutboxItem.MaxAttempts,
        FirstFailedAt = DateTime.UtcNow.AddMinutes(-20), LastError = "ProviderTimeout",
        CreatedAt = DateTime.UtcNow.AddMinutes(-21), NextAttemptAt = null
    };

    [Fact]
    public async Task Admin_lists_only_failed_emails_and_other_roles_are_rejected()
    {
        using var env = new ClientsEndpointTests.Env();
        env.AddUser("admin@dumbo.cl", UserRole.Admin, UserStatus.Active);
        env.AddUser("trab@dumbo.cl", UserRole.Worker, UserStatus.Active);
        var pending = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.PendingActivation, password: null);
        env.Outbox.Items.Add(FailedItem(pending));
        env.Outbox.Items.Add(EmailOutboxItem.Create(
            pending.Id, pending.Email, ActivationTokenType.AccountActivation, DateTime.UtcNow));
        using var admin = await env.SignInAsync("admin@dumbo.cl");
        using var worker = await env.SignInAsync("trab@dumbo.cl");
        using var anonymous = env.Factory.CreateClient();

        var body = await JsonAsync(await admin.GetAsync("/api/admin/email-outbox/failed"));
        var item = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal("ana@dumbo.cl", item.GetProperty("toEmail").GetString());
        Assert.Equal("AccountActivation", item.GetProperty("kind").GetString());
        Assert.Equal("ProviderTimeout", item.GetProperty("lastError").GetString());

        using var asWorker = await worker.GetAsync("/api/admin/email-outbox/failed");
        using var retryAsWorker = await worker.PostAsync($"/api/admin/email-outbox/{Guid.NewGuid()}/retry", null);
        using var noToken = await anonymous.GetAsync("/api/admin/email-outbox/failed");
        Assert.Equal(HttpStatusCode.Forbidden, asWorker.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, retryAsWorker.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
    }

    [Fact]
    public async Task Retrying_a_failed_email_resets_it_and_the_worker_delivers_it()
    {
        using var env = new ClientsEndpointTests.Env();
        env.AddUser("admin@dumbo.cl", UserRole.Admin, UserStatus.Active);
        var pending = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.PendingActivation, password: null);
        var failed = FailedItem(pending);
        env.Outbox.Items.Add(failed);
        using var admin = await env.SignInAsync("admin@dumbo.cl");

        using var retry = await admin.PostAsync($"/api/admin/email-outbox/{failed.Id}/retry", null);
        Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode);
        Assert.Equal(EmailOutboxStatus.Pending, failed.Status);
        Assert.Equal(0, failed.AttemptCount);
        Assert.Null(failed.FirstFailedAt);

        await env.RunWorkerOnceAsync();

        var message = Assert.Single(env.Sender.Sent);
        Assert.Equal("ana@dumbo.cl", message.To);
        Assert.Contains("/activar-cuenta?token=", message.TextBody);
        Assert.Equal(EmailOutboxStatus.Sent, failed.Status);

        // Already sent: cannot be retried again (prevents duplicate delivery)
        using var again = await admin.PostAsync($"/api/admin/email-outbox/{failed.Id}/retry", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("EMAIL_NOT_FAILED", (await JsonAsync(again)).GetProperty("code").GetString());

        using var unknown = await admin.PostAsync($"/api/admin/email-outbox/{Guid.NewGuid()}/retry", null);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("EMAIL_NOT_FOUND", (await JsonAsync(unknown)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_retried_email_is_cancelled_instead_of_sent_if_the_account_no_longer_needs_it()
    {
        using var env = new ClientsEndpointTests.Env();
        env.AddUser("admin@dumbo.cl", UserRole.Admin, UserStatus.Active);
        var active = env.AddUser("ana@dumbo.cl", UserRole.Client, UserStatus.Active);
        var failed = FailedItem(active);
        env.Outbox.Items.Add(failed);
        using var admin = await env.SignInAsync("admin@dumbo.cl");

        using var retry = await admin.PostAsync($"/api/admin/email-outbox/{failed.Id}/retry", null);
        Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode);
        await env.RunWorkerOnceAsync();

        Assert.Empty(env.Sender.Sent);
        Assert.Equal(EmailOutboxStatus.Cancelled, failed.Status);
    }
}