using Dumbo.Application.Abstractions;
using Dumbo.Application.Clients;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;
using Dumbo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dumbo.Api.Tests;

public sealed class AccountPersistenceTests
{
    [PostgresFact]
    public async Task Manual_account_creation_rolls_back_when_the_email_cannot_be_saved()
    {
        await using var factory = CreateFactory();
        var account = NewAccount(DateTime.UtcNow);
        var invalidEmail = EmailOutboxItem.Create(
            account.Id, new string('a', 256), ActivationTokenType.AccountActivation, account.CreatedAt);
        var cancellationToken = CancellationToken.None;

        await using (var write = factory.Services.CreateAsyncScope())
        {
            var db = write.ServiceProvider.GetRequiredService<DumboDbContext>();
            await db.Database.MigrateAsync(cancellationToken);
            var accounts = write.ServiceProvider.GetRequiredService<IAccountStore>();
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                accounts.TryAddAsync(account, cancellationToken, invalidEmail));
        }

        await using var verify = factory.Services.CreateAsyncScope();
        var persisted = verify.ServiceProvider.GetRequiredService<DumboDbContext>();
        Assert.False(await persisted.Users.AnyAsync(u => u.Id == account.Id, cancellationToken));
        Assert.False(await persisted.EmailOutbox.AnyAsync(e => e.Id == invalidEmail.Id, cancellationToken));

        var validEmail = EmailOutboxItem.Create(
            account.Id, account.Email, ActivationTokenType.AccountActivation, account.CreatedAt);
        var store = verify.ServiceProvider.GetRequiredService<IAccountStore>();
        Assert.True(await store.TryAddAsync(account, cancellationToken, validEmail));
        Assert.True(await persisted.Users.AnyAsync(u => u.Id == account.Id, cancellationToken));
        Assert.True(await persisted.EmailOutbox.AnyAsync(e => e.Id == validEmail.Id, cancellationToken));
    }

    [PostgresFact]
    public async Task Redeeming_requires_the_current_account_state_and_an_unused_link()
    {
        await using var factory = CreateFactory();
        var now = DateTime.UtcNow;
        var account = NewAccount(now);
        var link = new ActivationToken
        {
            UserId = account.Id,
            Type = ActivationTokenType.AccountActivation,
            Token = Guid.NewGuid().ToString("N"),
            CreatedAt = now,
            ExpiresAt = now + ActivationToken.Lifetime
        };
        var cancellationToken = CancellationToken.None;
        await using (var seed = factory.Services.CreateAsyncScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<DumboDbContext>();
            await db.Database.MigrateAsync(cancellationToken);
            db.AddRange(account, link);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IActivationTokenStore>();
        var persisted = scope.ServiceProvider.GetRequiredService<DumboDbContext>();

        Assert.False(await tokens.RedeemAsync(
            link.Id, account.Id, "new-hash", UserStatus.PendingActivation, Guid.NewGuid(), now, cancellationToken));
        Assert.False(await tokens.RedeemAsync(
            link.Id, account.Id, "new-hash", UserStatus.Active, account.SessionVersion, now, cancellationToken));
        Assert.False(await tokens.RedeemAsync(
            Guid.NewGuid(), account.Id, "new-hash", UserStatus.PendingActivation, account.SessionVersion, now, cancellationToken));

        var unchanged = await persisted.Users.AsNoTracking().SingleAsync(u => u.Id == account.Id, cancellationToken);
        Assert.Equal(UserStatus.PendingActivation, unchanged.Status);
        Assert.Equal(account.SessionVersion, unchanged.SessionVersion);
        Assert.Null(unchanged.PasswordHash);
        Assert.False(await persisted.ActivationTokens.Where(t => t.Id == link.Id)
            .Select(t => t.IsUsed).SingleAsync(cancellationToken));

        Assert.True(await tokens.RedeemAsync(
            link.Id, account.Id, "new-hash", UserStatus.PendingActivation, account.SessionVersion, now, cancellationToken));
        var activated = await persisted.Users.AsNoTracking().SingleAsync(u => u.Id == account.Id, cancellationToken);
        Assert.Equal(UserStatus.Active, activated.Status);
        Assert.Equal("new-hash", activated.PasswordHash);
        Assert.NotEqual(account.SessionVersion, activated.SessionVersion);
        Assert.True(await persisted.ActivationTokens.Where(t => t.Id == link.Id)
            .Select(t => t.IsUsed).SingleAsync(cancellationToken));
    }

    [PostgresFact]
    public async Task Deleting_an_account_expires_links_and_saves_the_audit()
    {
        await using var factory = CreateFactory();
        var now = DateTime.UtcNow.AddMinutes(-1);
        var account = NewAccount(now);
        var worker = NewAccount(now);
        worker.Role = UserRole.Worker;
        worker.Status = UserStatus.Active;
        var link = new ActivationToken
        {
            UserId = account.Id,
            Type = ActivationTokenType.AccountActivation,
            Token = Guid.NewGuid().ToString("N"),
            CreatedAt = now,
            ExpiresAt = now + ActivationToken.Lifetime
        };
        var cancellationToken = CancellationToken.None;
        await using (var seed = factory.Services.CreateAsyncScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<DumboDbContext>();
            await db.Database.MigrateAsync(cancellationToken);
            db.AddRange(account, worker, link);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using var staleScope = factory.Services.CreateAsyncScope();
        var staleStore = staleScope.ServiceProvider.GetRequiredService<IClientAdminStore>();
        var staleAccount = await staleStore.FindTrackedAsync(account.Id, cancellationToken);
        Assert.NotNull(staleAccount);

        await using (var write = factory.Services.CreateAsyncScope())
        {
            var lifecycle = write.ServiceProvider.GetRequiredService<ChangeAccountLifecycleUseCase>();
            var deletion = await lifecycle.ExecuteAsync(
                account.Id, worker.Id, AccountLifecycleAction.Delete, UserRole.Client, cancellationToken);
            Assert.Equal(AccountLifecycleOutcome.Changed, deletion.Outcome);
        }

        staleAccount.LastName = "Stale change";
        Assert.Equal(AccountSaveOutcome.ChangedConcurrently, await staleStore.SaveAsync(cancellationToken));

        await using var verify = factory.Services.CreateAsyncScope();
        var persisted = verify.ServiceProvider.GetRequiredService<DumboDbContext>();
        var deleted = await persisted.Users.SingleAsync(u => u.Id == account.Id, cancellationToken);
        Assert.Equal(UserStatus.Deleted, deleted.Status);
        Assert.Equal(account.LastName, deleted.LastName);
        Assert.NotEqual(account.SessionVersion, deleted.SessionVersion);
        var audit = await persisted.AccountAudit.SingleAsync(a => a.TargetUserId == account.Id, cancellationToken);
        Assert.Equal(worker.Id, audit.ActorUserId);
        Assert.Equal(UserStatus.PendingActivation, audit.PreviousStatus);
        Assert.Equal(UserStatus.Deleted, audit.NewStatus);
        var expired = await persisted.ActivationTokens.SingleAsync(t => t.Id == link.Id, cancellationToken);
        Assert.Equal(audit.OccurredAt, expired.ExpiresAt);
    }

    private static User NewAccount(DateTime now) => new()
    {
        Email = $"account-{Guid.NewGuid():N}@example.test",
        FirstName = "Integration",
        LastName = "Test",
        Phone = "+56912345678",
        CreatedAt = now
    };

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Dumbo"] = Environment.GetEnvironmentVariable("DUMBO_TEST_DATABASE"),
                    ["Email:WorkerEnabled"] = "false"
                }));
        });
}
