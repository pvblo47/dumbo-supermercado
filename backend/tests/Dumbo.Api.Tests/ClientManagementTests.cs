using Dumbo.Application.Clients;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Api.Tests;

/// <summary>
/// Unit tests for client administration use cases without HTTP overhead
/// </summary>
public sealed class ClientManagementTests
{
    private sealed class Fixture
    {
        public FakeAccountLookup Users { get; } = new();
        public InMemoryOutbox Outbox { get; } = new();
        public InMemoryClientAdminStore Clients { get; }
        public LookupBackedAccountStore Accounts { get; }
        public TimeProvider Clock { get; } = new FixedClock(new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero));

        public Fixture()
        {
            Clients = new InMemoryClientAdminStore(Users);
            Accounts = new LookupBackedAccountStore(Users);
        }

        public User Add(UserStatus status, string email = "ana@mail.com", string? passwordHash = "hash:x")
        {
            var user = new User
            {
                Email = email, FirstName = "Ana", LastName = "Soto", Phone = "+56912345678",
                Role = UserRole.Client, Status = status, PasswordHash = passwordHash,
                CreatedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            Users.Users.Add(user);
            return user;
        }
    }

    // Pending account: changing the email must send the activation link to the normalized new address.
    [Fact]
    public async Task Changing_the_email_of_a_pending_account_queues_a_new_activation_email()
    {
        var f = new Fixture();
        var user = f.Add(UserStatus.PendingActivation, passwordHash: null);
        var useCase = new UpdateClientUseCase(f.Clients, f.Accounts, f.Outbox, f.Clock);

        var result = await useCase.ExecuteAsync(
            new UpdateClientCommand(user.Id, "  NUEVO@Mail.com ", "Ana", "Soto", "+56912345678"), CancellationToken.None);

        Assert.Equal(UpdateClientOutcome.Updated, result.Outcome);
        Assert.Equal("nuevo@mail.com", user.Email);
        var queued = Assert.Single(f.Outbox.Items);
        Assert.Equal(ActivationTokenType.AccountActivation, queued.Kind);
        Assert.Equal("nuevo@mail.com", queued.ToEmail);
    }

    // Updating the same account without changing its email is valid and skips extra activation mail.
    [Fact]
    public async Task Editing_without_changing_the_email_is_valid_and_queues_no_mail()
    {
        var f = new Fixture();
        var user = f.Add(UserStatus.Active);
        var useCase = new UpdateClientUseCase(f.Clients, f.Accounts, f.Outbox, f.Clock);

        var result = await useCase.ExecuteAsync(
            new UpdateClientCommand(user.Id, "ana@mail.com", "Ana María", "Soto", "+56999999999"), CancellationToken.None);

        Assert.Equal(UpdateClientOutcome.Updated, result.Outcome);
        Assert.Equal("Ana María", user.FirstName);
        Assert.Equal("+56999999999", user.Phone);
        Assert.NotNull(user.UpdatedAt);
        Assert.Empty(f.Outbox.Items);
    }

    [Fact]
    public async Task Habilitating_a_disabled_account_returns_it_to_active_or_pending_depending_on_its_password()
    {
        var f = new Fixture();
        var withPassword = f.Add(UserStatus.Disabled, "con@mail.com", "hash:x");
        var withoutPassword = f.Add(UserStatus.Disabled, "sin@mail.com", null);
        var useCase = new ChangeClientStatusUseCase(f.Clients, f.Outbox, f.Clock);

        await useCase.ExecuteAsync(withPassword.Id, ClientStatusAction.Enable, CancellationToken.None);
        await useCase.ExecuteAsync(withoutPassword.Id, ClientStatusAction.Enable, CancellationToken.None);

        Assert.Equal(UserStatus.Active, withPassword.Status);
        Assert.Equal(UserStatus.PendingActivation, withoutPassword.Status);
    }

    [Fact]
    public async Task List_corrects_the_page_size_and_rejects_unknown_or_deleted_statuses()
    {
        var f = new Fixture();
        f.Add(UserStatus.Active);
        var useCase = new ListClientsUseCase(f.Clients);

        var big = await useCase.ExecuteAsync(new ListClientsCommand(null, null, 0, 1000), CancellationToken.None);
        Assert.Equal(ListClientsUseCase.MaxPageSize, big.Page!.PageSize);
        Assert.Equal(1, big.Page.Page);

        foreach (var invalid in new[] { "Deleted", "xyz", "2" })
        {
            var result = await useCase.ExecuteAsync(new ListClientsCommand(null, invalid, null, null), CancellationToken.None);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors!.ContainsKey("status"));
        }
    }

    /// <summary>
    /// Verifies that enabling a disabled account with no password hash transitions it to
    /// <see cref="UserStatus.PendingActivation"/> rather than active, and queues a single
    /// <see cref="ActivationTokenType.AccountActivation"/> email (D07.6).
    /// </summary>
    [Fact]
    public async Task Habilitating_an_account_that_never_activated_stays_pending_and_resends_the_activation()
    {
        var f = new Fixture();
        var user = f.Add(UserStatus.Disabled, "sin@mail.com", passwordHash: null);
        var useCase = new ChangeClientStatusUseCase(f.Clients, f.Outbox, f.Clock);

        var result = await useCase.ExecuteAsync(user.Id, ClientStatusAction.Enable, CancellationToken.None);

        Assert.Equal(UserStatus.PendingActivation, user.Status);   // Does not become active directly
        Assert.Equal(nameof(UserStatus.PendingActivation), result.Account!.Status);
        var queued = Assert.Single(f.Outbox.Items);
        Assert.Equal(ActivationTokenType.AccountActivation, queued.Kind);
    }
}