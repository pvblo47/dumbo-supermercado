using Dumbo.Application.Accounts;
using Dumbo.Application.Email;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Api.Tests;

public sealed class EmailOutboxProcessingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);

    private sealed class Fixture
    {
        public FakeAccountLookup Users { get; } = new();
        public InMemoryOutbox Outbox { get; } = new();
        public CapturingEmailSender Sender { get; } = new();
        public MutableClock Clock { get; } = new(T0);
        public InMemoryTokenStore Tokens { get; }
        public ProcessEmailOutboxUseCase UseCase { get; }

        public Fixture()
        {
            Tokens = new InMemoryTokenStore(Users);
            UseCase = new ProcessEmailOutboxUseCase(
                Outbox, Users, Tokens, Sender, new EmailLinkSettings("http://localhost:8080"), Clock);
        }

        public EmailOutboxItem Enqueue(UserStatus status, ActivationTokenType kind)
        {
            var user = new User
            {
                Email = "ana@mail.com", FirstName = "Ana", LastName = "Pérez", Phone = "+56912345678",
                Role = UserRole.Client, Status = status, CreatedAt = T0.UtcDateTime
            };
            Users.Users.Add(user);

            var item = EmailOutboxItem.Create(user.Id, user.Email, kind, T0.UtcDateTime);
            Outbox.Items.Add(item);
            return item;
        }

        public Task RunAsync() => UseCase.ExecuteAsync(10, CancellationToken.None);
    }

    [Fact]
    public async Task Sends_a_reset_email_with_a_fresh_30_minute_link_and_stores_only_its_hash()
    {
        var f = new Fixture();
        var item = f.Enqueue(UserStatus.Active, ActivationTokenType.PasswordReset);

        await f.RunAsync();

        var message = Assert.Single(f.Sender.Sent);
        Assert.Equal("ana@mail.com", message.To);
        Assert.Contains("http://localhost:8080/restablecer-contrasena?token=", message.TextBody);
        Assert.DoesNotContain("Contraseña:", message.TextBody);  

        var raw = CapturingEmailSender.ExtractToken(message);
        var stored = Assert.Single(f.Tokens.Tokens);
        Assert.Equal(SecureToken.Hash(raw), stored.Token);
        Assert.NotEqual(raw, stored.Token);                   
        Assert.Equal(T0.UtcDateTime.AddMinutes(30), stored.ExpiresAt);      
        Assert.Equal(EmailOutboxStatus.Sent, item.Status);
    }

    [Fact]
    public async Task Activation_email_points_to_the_activation_page()
    {
        var f = new Fixture();
        f.Enqueue(UserStatus.PendingActivation, ActivationTokenType.AccountActivation);

        await f.RunAsync();

        Assert.Contains("http://localhost:8080/activar-cuenta?token=", Assert.Single(f.Sender.Sent).TextBody);
    }

    [Fact]
    public async Task Failed_sends_are_retried_at_minutes_1_5_and_15_and_then_marked_failed()
    {
        var f = new Fixture();
        f.Sender.FailuresToSimulate = 10;
        var item = f.Enqueue(UserStatus.Active, ActivationTokenType.PasswordReset);

        await f.RunAsync();           
        Assert.Equal(1, item.AttemptCount);
        Assert.Equal(T0.UtcDateTime.AddMinutes(1), item.NextAttemptAt);

        f.Clock.Now = T0.AddSeconds(30);
        await f.RunAsync();            
        Assert.Equal(1, item.AttemptCount);

        f.Clock.Now = T0.AddMinutes(1);
        await f.RunAsync();   
        Assert.Equal(2, item.AttemptCount);
        Assert.Equal(T0.UtcDateTime.AddMinutes(5), item.NextAttemptAt);

        f.Clock.Now = T0.AddMinutes(5);
        await f.RunAsync();    
        Assert.Equal(3, item.AttemptCount);
        Assert.Equal(T0.UtcDateTime.AddMinutes(15), item.NextAttemptAt);

        f.Clock.Now = T0.AddMinutes(15);
        await f.RunAsync();                   
        Assert.Equal(4, item.AttemptCount);
        Assert.Equal(EmailOutboxStatus.Failed, item.Status);
        Assert.Null(item.NextAttemptAt);

        f.Clock.Now = T0.AddHours(1);
        await f.RunAsync();    
        Assert.Equal(4, item.AttemptCount);
        Assert.Empty(f.Sender.Sent);
        Assert.Equal("Proveedor no disponible", item.LastError);
    }

    [Fact]
    public async Task A_successful_retry_stops_further_attempts_and_invalidates_the_previous_link()
    {
        var f = new Fixture();
        f.Sender.FailuresToSimulate = 1;
        var item = f.Enqueue(UserStatus.Active, ActivationTokenType.PasswordReset);

        await f.RunAsync();
        f.Clock.Now = T0.AddMinutes(1);
        await f.RunAsync();                    

        Assert.Equal(EmailOutboxStatus.Sent, item.Status);
        Assert.Equal(2, item.AttemptCount);
        Assert.Single(f.Sender.Sent);
        Assert.Equal(2, f.Tokens.Tokens.Count);
        Assert.Equal(1, f.Tokens.Tokens.Count(t => t.IsValidAt(f.Clock.Now.UtcDateTime)));

        f.Clock.Now = T0.AddMinutes(2);
        await f.RunAsync();
        Assert.Single(f.Sender.Sent);
    }

    [Fact]
    public async Task An_item_is_cancelled_without_sending_if_the_account_is_no_longer_eligible()
    {
        var f = new Fixture();
        var item = f.Enqueue(UserStatus.Active, ActivationTokenType.PasswordReset);
        f.Users.Users.Single().Status = UserStatus.Disabled;

        await f.RunAsync();

        Assert.Equal(EmailOutboxStatus.Cancelled, item.Status);
        Assert.Empty(f.Sender.Sent);
    }
}