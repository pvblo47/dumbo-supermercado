using Dumbo.Application.Accounts;
using Dumbo.Domain.Enums;
using Dumbo.Domain.Rules;


namespace Dumbo.Api.Tests;

/// <summary>
/// Unit tests for pure account domain rules (password policy, email validation, and email normalization)
/// covering RF-001 and RF-002 without HTTP or database dependencies.
/// </summary>
public sealed class AccountRulesTests
{
    // ERS password policy: minimum 9 characters, at least one uppercase letter,
    // one lowercase letter, and one digit. Each case targets a specific boundary or rule.
    /// <summary>
    /// Verifies that password validation enforces the ERS complexity and length requirements.
    /// </summary>
    [Theory]
    [InlineData("Abcdefgh1", true)]    // cumple todo (9 caracteres)
    [InlineData("Abcdefg1", false)]    // solo 8 caracteres
    [InlineData("abcdefgh1", false)]   // sin mayúscula
    [InlineData("ABCDEFGH1", false)]   // sin minúscula
    [InlineData("Abcdefghi", false)]   // sin número
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Password_policy_follows_the_ers(string? password, bool expected) =>
        Assert.Equal(expected, AccountRules.IsValidPassword(password));

    // Expects an already normalized email (trimmed and lowercased).
    /// <summary>
    /// Verifies email format validation rules on normalized addresses.
    /// </summary>
    [Theory]
    [InlineData("ana@mail.com", true)]
    [InlineData("ana@mail", false)]          // dominio sin punto
    [InlineData("ana@", false)]              // falta el dominio
    [InlineData("ana mail@x.com", false)]    // espacio dentro del correo
    [InlineData("ana <a@b.com>", false)]     // formato "Nombre <correo>" no permitido
    [InlineData("", false)]
    public void Email_format_is_validated(string normalizedEmail, bool expected) =>
        Assert.Equal(expected, AccountRules.IsValidEmail(normalizedEmail));

    /// <summary>
    /// Verifies that <see cref="AccountRules.NormalizeEmail"/> trims outer whitespace and lowercases input.
    /// </summary>
    [Fact]
    public void Email_is_trimmed_and_lowercased() =>
        Assert.Equal("ana@mail.com", AccountRules.NormalizeEmail("  Ana@Mail.COM "));
}

/// <summary>
/// Unit tests for the client self-registration use case (RF-002) using in-memory test fakes
/// without touching PostgreSQL.
/// </summary>
public sealed class RegisterClientUseCaseTests
{
    // Fixed UTC timestamp to deterministically assert CreatedAt.
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static RegisterClientUseCase CreateUseCase(FakeAccountStore store) =>
        new(store, new FakePasswordHasher(), new FixedClock(Now));

    // Intentionally "dirty" input (extra spaces and mixed casing) to verify normalization.
    private static RegisterClientCommand Valid() =>
        new("  Ana.Perez@Mail.COM ", "  Ana María ", "Pérez Soto", "+56 9 1234 5678", "Segura1234");

    /// <summary>
    /// Verifies that valid registration input creates an active Client account with normalized fields,
    /// a non-plaintext password hash, and the expected creation timestamp.
    /// </summary>
    [Fact]
    public async Task Creates_an_active_client_with_normalized_email_and_hashed_password()
    {
        var store = new FakeAccountStore();

        var result = await CreateUseCase(store).ExecuteAsync(Valid(), CancellationToken.None);

        Assert.Equal(RegisterClientOutcome.Created, result.Outcome);

        var user = Assert.Single(store.Added);
        Assert.Equal("ana.perez@mail.com", user.Email);   // sin espacios y en minúsculas
        Assert.Equal("Ana María", user.FirstName);        // sin espacios exteriores
        Assert.Equal(UserRole.Client, user.Role);         // el autorregistro solo crea Clientes
        Assert.Equal(UserStatus.Active, user.Status);     // el cliente define su clave: queda activo
        Assert.Equal("hashed-value", user.PasswordHash);  // nunca la contraseña en claro
        Assert.Equal(Now.UtcDateTime, user.CreatedAt);
        Assert.Equal("Client", result.Account!.Role);
    }

    /// <summary>
    /// Verifies that blank or invalid fields and weak passwords return per-field errors
    /// (including the literal ERS password message) without persisting any user.
    /// </summary>
    [Fact]
    public async Task Rejects_blank_fields_and_weak_password_per_field_without_saving()
    {
        var store = new FakeAccountStore();
        // Whitespace-only email/phone, empty/null names, weak password.
        var command = new RegisterClientCommand(" ", "", null, "  ", "corta");

        var result = await CreateUseCase(store).ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(RegisterClientOutcome.Invalid, result.Outcome);

        // Must report an error per field so the frontend can display it next to each input.
        Assert.Equal(
            new[] { "email", "firstName", "lastName", "password", "phone" },
            result.Errors!.Keys.Order().ToArray());

        // Literal message required by the ERS (RF-001/RF-002).
        Assert.Equal(
            "La nueva contraseña no cumple los estándares de seguridad",
            result.Errors["password"][0]);

        Assert.Empty(store.Added);   // con errores no se guarda nada
    }

    /// <summary>
    /// Verifies that an already registered email is rejected regardless of surrounding spaces and casing.
    /// </summary>
    [Fact]
    public async Task Rejects_an_email_already_registered_ignoring_case_and_spaces()
    {
        var store = new FakeAccountStore();
        store.ExistingEmails.Add("ana.perez@mail.com");

        // Valid() provides the same email with extra whitespace and mixed casing.
        var result = await CreateUseCase(store).ExecuteAsync(Valid(), CancellationToken.None);

        Assert.Equal(RegisterClientOutcome.EmailTaken, result.Outcome);
        Assert.Empty(store.Added);
    }

    // Two concurrent registrations with the same email both pass the pre-check,
    // but only one succeeds on insert; the other must receive EmailTaken rather than a 500 error.
    /// <summary>
    /// Verifies that a concurrent insert losing the uniqueness race returns <see cref="RegisterClientOutcome.EmailTaken"/>.
    /// </summary>
    [Fact]
    public async Task Reports_email_taken_when_a_concurrent_registration_wins()
    {
        var store = new FakeAccountStore { LoseRaceOnAdd = true };

        var result = await CreateUseCase(store).ExecuteAsync(Valid(), CancellationToken.None);

        Assert.Equal(RegisterClientOutcome.EmailTaken, result.Outcome);
    }
}