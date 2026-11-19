using Dumbo.Application.Accounts;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Api.Tests;

/// <summary>
/// Pruebas de "olvidé mi contraseña" y de definir la contraseña con un enlace
/// </summary>
public sealed class PasswordRecoveryUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);

    private static User MakeUser(UserStatus status) => new()
    {
        Email = "ana@mail.com", FirstName = "Ana", LastName = "Pérez", Phone = "+56912345678",
        PasswordHash = status == UserStatus.PendingActivation ? null : "hash:Antigua123",
        Role = UserRole.Client, Status = status, CreatedAt = Now.UtcDateTime
    };

    private sealed class Fixture
    {
        public FakeAccountLookup Users { get; } = new();
        public InMemoryOutbox Outbox { get; } = new();
        public InMemoryTokenStore Tokens { get; }
        public RequestPasswordRecoveryUseCase Request { get; }
        public SetPasswordWithTokenUseCase SetPassword { get; }

        public Fixture()
        {
            var clock = new FixedClock(Now);
            Tokens = new InMemoryTokenStore(Users);
            Request = new RequestPasswordRecoveryUseCase(Users, Outbox, clock);
            SetPassword = new SetPasswordWithTokenUseCase(Tokens, new PrefixPasswordHasher(), clock);
        }

        public User AddUser(UserStatus status)
        {
            var user = MakeUser(status);
            Users.Users.Add(user);
            return user;
        }

        public ActivationToken AddToken(User user, ActivationTokenType type, string raw, TimeSpan? validFor = null)
        {
            var token = new ActivationToken
            {
                UserId = user.Id, Type = type, Token = SecureToken.Hash(raw), CreatedAt = Now.UtcDateTime,
                ExpiresAt = Now.UtcDateTime + (validFor ?? TimeSpan.FromMinutes(30))
            };
            Tokens.Tokens.Add(token);
            return token;
        }
    }


    [Fact]
    public async Task Active_account_gets_a_password_reset_email_queued()
    {
        var f = new Fixture();
        f.AddUser(UserStatus.Active);

        await f.Request.ExecuteAsync("  ANA@Mail.com ", CancellationToken.None);   // se normaliza el correo

        var item = Assert.Single(f.Outbox.Items);
        Assert.Equal(ActivationTokenType.PasswordReset, item.Kind);
        Assert.Equal("ana@mail.com", item.ToEmail);
    }

    [Fact]
    public async Task Pending_account_gets_its_activation_email_queued()
    {
        var f = new Fixture();
        f.AddUser(UserStatus.PendingActivation);

        await f.Request.ExecuteAsync("ana@mail.com", CancellationToken.None);

        Assert.Equal(ActivationTokenType.AccountActivation, Assert.Single(f.Outbox.Items).Kind);
    }

    [Theory]
    [InlineData(UserStatus.Disabled)]
    [InlineData(UserStatus.Deleted)]
    public async Task Disabled_or_deleted_accounts_get_no_email(UserStatus status)
    {
        var f = new Fixture();
        f.AddUser(status);

        await f.Request.ExecuteAsync("ana@mail.com", CancellationToken.None);

        Assert.Empty(f.Outbox.Items);
    }

    [Theory]
    [InlineData("nadie@mail.com")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Unknown_or_blank_email_queues_nothing(string? email)
    {
        var f = new Fixture();
        f.AddUser(UserStatus.Active);

        await f.Request.ExecuteAsync(email, CancellationToken.None);

        Assert.Empty(f.Outbox.Items);
    }

    [Fact]
    public async Task Repeated_requests_do_not_stack_emails_while_one_is_pending()
    {
        var f = new Fixture();
        f.AddUser(UserStatus.Active);

        await f.Request.ExecuteAsync("ana@mail.com", CancellationToken.None);
        await f.Request.ExecuteAsync("ana@mail.com", CancellationToken.None);

        Assert.Single(f.Outbox.Items);
    }

    // Password

    [Fact]
    public async Task Activation_link_sets_the_password_and_activates_a_pending_account()
    {
        var f = new Fixture();
        var user = f.AddUser(UserStatus.PendingActivation);
        f.AddToken(user, ActivationTokenType.AccountActivation, "abc");

        var result = await f.SetPassword.ExecuteAsync(
            new SetPasswordCommand(ActivationTokenType.AccountActivation, "abc", "Nueva12345"), CancellationToken.None);

        Assert.Equal(SetPasswordOutcome.Success, result.Outcome);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal("hash:Nueva12345", user.PasswordHash);
    }

    [Fact]
    public async Task Reset_link_changes_the_password_of_an_active_account()
    {
        var f = new Fixture();
        var user = f.AddUser(UserStatus.Active);
        f.AddToken(user, ActivationTokenType.PasswordReset, "abc");

        var result = await f.SetPassword.ExecuteAsync(
            new SetPasswordCommand(ActivationTokenType.PasswordReset, "abc", "Nueva12345"), CancellationToken.None);

        Assert.Equal(SetPasswordOutcome.Success, result.Outcome);
        Assert.Equal("hash:Nueva12345", user.PasswordHash);
    }

    // Una contraseña débil se rechaza con el mensaje literal del ERS SIN gastar el enlace
    [Fact]
    public async Task Weak_password_is_rejected_and_the_link_is_not_consumed()
    {
        var f = new Fixture();
        var user = f.AddUser(UserStatus.Active);
        var token = f.AddToken(user, ActivationTokenType.PasswordReset, "abc");

        var result = await f.SetPassword.ExecuteAsync(
            new SetPasswordCommand(ActivationTokenType.PasswordReset, "abc", "debil"), CancellationToken.None);

        Assert.Equal(SetPasswordOutcome.InvalidPassword, result.Outcome);
        Assert.Equal("La nueva contraseña no cumple los estándares de seguridad", result.Errors!["password"][0]);
        Assert.False(token.IsUsed);
        Assert.Equal("hash:Antigua123", user.PasswordHash);
    }

    [Fact]
    public async Task A_link_can_only_be_used_once()
    {
        var f = new Fixture();
        var user = f.AddUser(UserStatus.Active);
        f.AddToken(user, ActivationTokenType.PasswordReset, "abc");
        var command = new SetPasswordCommand(ActivationTokenType.PasswordReset, "abc", "Nueva12345");

        var first = await f.SetPassword.ExecuteAsync(command, CancellationToken.None);
        var second = await f.SetPassword.ExecuteAsync(command with { Password = "Otra123456" }, CancellationToken.None);

        Assert.Equal(SetPasswordOutcome.Success, first.Outcome);
        Assert.Equal(SetPasswordOutcome.InvalidToken, second.Outcome);
        Assert.Equal("hash:Nueva12345", user.PasswordHash);  
    }

    [Fact]
    public async Task An_expired_link_is_rejected()
    {
        var f = new Fixture();
        var user = f.AddUser(UserStatus.Active);
        f.AddToken(user, ActivationTokenType.PasswordReset, "abc", validFor: TimeSpan.FromMinutes(-1));

        var result = await f.SetPassword.ExecuteAsync(
            new SetPasswordCommand(ActivationTokenType.PasswordReset, "abc", "Nueva12345"), CancellationToken.None);

        Assert.Equal(SetPasswordOutcome.InvalidToken, result.Outcome);
    }

    [Fact]
    public async Task A_link_of_the_other_type_or_an_unknown_one_is_rejected()
    {
        var f = new Fixture();
        var user = f.AddUser(UserStatus.PendingActivation);
        f.AddToken(user, ActivationTokenType.AccountActivation, "abc");

        var wrongType = await f.SetPassword.ExecuteAsync(
            new SetPasswordCommand(ActivationTokenType.PasswordReset, "abc", "Nueva12345"), CancellationToken.None);
        var unknown = await f.SetPassword.ExecuteAsync(
            new SetPasswordCommand(ActivationTokenType.AccountActivation, "no-existe", "Nueva12345"), CancellationToken.None);

        Assert.Equal(SetPasswordOutcome.InvalidToken, wrongType.Outcome);
        Assert.Equal(SetPasswordOutcome.InvalidToken, unknown.Outcome);
        Assert.Equal(UserStatus.PendingActivation, user.Status);
    }

    [Theory]
    [InlineData(UserStatus.Disabled, ActivationTokenType.AccountActivation)]
    [InlineData(UserStatus.Disabled, ActivationTokenType.PasswordReset)]
    [InlineData(UserStatus.Deleted, ActivationTokenType.PasswordReset)]
    public async Task A_valid_link_never_reactivates_a_disabled_or_deleted_account(
        UserStatus status, ActivationTokenType type)
    {
        var f = new Fixture();
        var user = f.AddUser(status);
        f.AddToken(user, type, "abc");

        var result = await f.SetPassword.ExecuteAsync(
            new SetPasswordCommand(type, "abc", "Nueva12345"), CancellationToken.None);

        Assert.Equal(SetPasswordOutcome.InvalidToken, result.Outcome);
        Assert.Equal(status, user.Status);
        Assert.Equal("hash:Antigua123", user.PasswordHash);
    }
}