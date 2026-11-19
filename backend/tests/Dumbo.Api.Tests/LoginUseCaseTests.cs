using Dumbo.Application.Accounts;
using Dumbo.Domain.Entities;
using Dumbo.Domain.Enums;

namespace Dumbo.Api.Tests;

/// <summary>
/// Unit tests for the sign-in use case without HTTP or a database.
/// Primarily verifies literal SRS error messages and allowed account states.
/// </summary>
public sealed class LoginUseCaseTests
{
    private const string Password = "Segura1234";

    private static LoginUseCase CreateUseCase(FakeAccountLookup lookup) =>
        new(lookup, new PrefixPasswordHasher(), new FakeAccessTokenIssuer());

    // Test account using password "Segura1234". PendingActivation accounts have no password (D06).
    private static User MakeUser(UserStatus status) => new()
    {
        Email = "ana@mail.com",
        FirstName = "Ana",
        LastName = "Pérez",
        Phone = "+56912345678",
        PasswordHash = status == UserStatus.PendingActivation ? null : "hash:" + Password,
        Role = UserRole.Client,
        Status = status,
        CreatedAt = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc)
    };

    private static FakeAccountLookup LookupWith(User user)
    {
        var lookup = new FakeAccountLookup();
        lookup.Users.Add(user);
        return lookup;
    }

    [Fact]
    public async Task Active_account_with_correct_password_gets_a_token_ignoring_email_case()
    {
        var useCase = CreateUseCase(LookupWith(MakeUser(UserStatus.Active)));

        var result = await useCase.ExecuteAsync(
            new LoginCommand("  ANA@Mail.com ", Password), CancellationToken.None);

        Assert.Equal(LoginOutcome.Success, result.Outcome);
        Assert.Equal("token-for-ana@mail.com", result.Session!.AccessToken);
        Assert.Equal("Bearer", result.Session.TokenType);
        Assert.Equal("Client", result.Session.Account.Role);
    }

    [Fact]
    public async Task Wrong_password_returns_the_generic_ers_message()
    {
        var useCase = CreateUseCase(LookupWith(MakeUser(UserStatus.Active)));

        var result = await useCase.ExecuteAsync(
            new LoginCommand("ana@mail.com", "OtraClave123"), CancellationToken.None);

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Equal(
            "Las credenciales de acceso son incorrectas o el usuario no está registrado en el sistema",
            result.Message);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task Unknown_email_returns_the_same_generic_message()
    {
        var useCase = CreateUseCase(new FakeAccountLookup());

        var result = await useCase.ExecuteAsync(
            new LoginCommand("nadie@mail.com", Password), CancellationToken.None);

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Equal(LoginUseCase.InvalidCredentialsMessage, result.Message);
    }

    // A deleted account uses the generic message even when the password is valid.
    [Fact]
    public async Task Deleted_account_cannot_log_in_and_gets_the_generic_message()
    {
        var useCase = CreateUseCase(LookupWith(MakeUser(UserStatus.Deleted)));

        var result = await useCase.ExecuteAsync(
            new LoginCommand("ana@mail.com", Password), CancellationToken.None);

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Null(result.Session);
    }

    // manually provisioned accounts cannot sign in until a password is set.
    [Fact]
    public async Task Pending_activation_account_cannot_log_in()
    {
        var useCase = CreateUseCase(LookupWith(MakeUser(UserStatus.PendingActivation)));

        var result = await useCase.ExecuteAsync(
            new LoginCommand("ana@mail.com", Password), CancellationToken.None);

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public async Task Disabled_account_with_correct_password_gets_the_disabled_message()
    {
        var useCase = CreateUseCase(LookupWith(MakeUser(UserStatus.Disabled)));

        var result = await useCase.ExecuteAsync(
            new LoginCommand("ana@mail.com", Password), CancellationToken.None);

        Assert.Equal(LoginOutcome.Disabled, result.Outcome);
        Assert.Equal("Usuario deshabilitado", result.Message);
        Assert.Null(result.Session);
    }

    // Security: wrong credentials must not disclose whether the account exists or is disabled.
    [Fact]
    public async Task Disabled_account_with_wrong_password_does_not_reveal_its_state()
    {
        var useCase = CreateUseCase(LookupWith(MakeUser(UserStatus.Disabled)));

        var result = await useCase.ExecuteAsync(
            new LoginCommand("ana@mail.com", "OtraClave123"), CancellationToken.None);

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", Password)]
    [InlineData("   ", Password)]
    [InlineData("ana@mail.com", "")]
    [InlineData("ana@mail.com", null)]
    public async Task Missing_fields_return_the_generic_message(string? email, string? password)
    {
        var useCase = CreateUseCase(LookupWith(MakeUser(UserStatus.Active)));

        var result = await useCase.ExecuteAsync(new LoginCommand(email, password), CancellationToken.None);

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
    }
}