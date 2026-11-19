using Dumbo.Application.Abstractions;
using Dumbo.Domain.Enums;
using Dumbo.Domain.Rules;

namespace Dumbo.Application.Accounts;

/// <summary>Input data for sign-in. May be empty and is validated here.</summary>
public sealed record LoginCommand(string? Email, string? Password);

/// <summary>Session returned upon successful sign-in. Angular stores the token and sends it on each request.</summary>
public sealed record AuthSession(string AccessToken, string TokenType, DateTime ExpiresAt, AccountSummary Account);

public enum LoginOutcome { Success, InvalidCredentials, Disabled }

/// <summary>Sign-in outcome: <see cref="Session"/> is populated only on Success; <see cref="Message"/> only on failures.</summary>
public sealed record LoginResult(LoginOutcome Outcome, string? Message = null, AuthSession? Session = null);

/// <summary>
/// Use case: sign in with email and password.
/// Only an Active account receives a token; all others are rejected with the message mandated by the ERS.
/// </summary>
public sealed class LoginUseCase(
    IAccountLookup lookup, IAccountPasswordHasher hasher, IAccessTokenIssuer tokens)
{
    /// <summary>Literal RF-001 message for invalid credentials, non-existent user, or deleted account.</summary>
    public const string InvalidCredentialsMessage =
        "Las credenciales de acceso son incorrectas o el usuario no está registrado en el sistema";

    /// <summary>Literal RF-001 message for a disabled account.</summary>
    public const string DisabledMessage = "Usuario deshabilitado";

    public async Task<LoginResult> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        // Empty fields: same generic message, without hints about what is missing.
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrEmpty(command.Password))
            return Invalid();

        var email = AccountRules.NormalizeEmail(command.Email);
        var user = await lookup.FindByEmailAsync(email, cancellationToken);

        // Missing account, or account without a password set (Pending activation, D06): generic response.
        // Known limitation: when the email does not exist, the response is slightly faster than
        // when it exists (no hash is computed). Can be hardened later.
        if (user is null || user.PasswordHash is null)
            return Invalid();

        // Password is verified BEFORE revealing account status: this way "Usuario
        // deshabilitado" is only shown to someone who knows the password, not anyone guessing an email.
        if (!hasher.Verify(user.PasswordHash, command.Password))
            return Invalid();

        switch (user.Status)
        {
            case UserStatus.Active:
            {
                var token = tokens.Issue(user);
                return new LoginResult(
                    LoginOutcome.Success,
                    Session: new AuthSession(token.Value, "Bearer", token.ExpiresAtUtc, user.ToSummary()));
            }

            case UserStatus.Disabled:
                return new LoginResult(LoginOutcome.Disabled, DisabledMessage);

            // Deleted or Pending activation: does not sign in and is not reactivated by attempting login.
            default:
                return Invalid();
        }
    }

    private static LoginResult Invalid() =>
        new(LoginOutcome.InvalidCredentials, InvalidCredentialsMessage);
}