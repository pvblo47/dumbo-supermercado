using Dumbo.Api.Contracts;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

/// <summary>
/// Handles account activation and password recovery via email links
/// All endpoints are public because callers cannot sign in yet when invoking them.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AccountAccessController : ControllerBase
{
    /// <summary>
    /// Requests a password recovery or activation link. Always returns <c>202 Accepted</c> regardless
    /// of whether the email exists to avoid account enumeration; delivery happens asynchronously via background worker.
    /// </summary>
    [HttpPost("password/forgot")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Forgot(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] RequestPasswordRecoveryUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(request.Email, cancellationToken);

        return StatusCode(StatusCodes.Status202Accepted,
            new { message = "Si el correo está registrado, recibirás un enlace en los próximos minutos." });
    }

    /// <summary>Sets a new password using a password-recovery token link.</summary>
    [HttpPost("password/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Reset(
        [FromBody] SetPasswordRequest request,
        [FromServices] SetPasswordWithTokenUseCase useCase,
        CancellationToken cancellationToken) =>
        SetPasswordAsync(ActivationTokenType.PasswordReset, request, useCase, cancellationToken);

    /// <summary>Sets the initial password using an account-activation token link and marks the account Active.</summary>
    [HttpPost("activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Activate(
        [FromBody] SetPasswordRequest request,
        [FromServices] SetPasswordWithTokenUseCase useCase,
        CancellationToken cancellationToken) =>
        SetPasswordAsync(ActivationTokenType.AccountActivation, request, useCase, cancellationToken);

    private async Task<IActionResult> SetPasswordAsync(
        ActivationTokenType type, SetPasswordRequest request, SetPasswordWithTokenUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new SetPasswordCommand(type, request.Token, request.Password), cancellationToken);

        switch (result.Outcome)
        {
            case SetPasswordOutcome.Success:
                return Ok(new { message = "Contraseña definida. Ya puedes iniciar sesión." });

            case SetPasswordOutcome.InvalidPassword:
            {
                var problem = new ValidationProblemDetails(result.Errors!.ToDictionary(e => e.Key, e => e.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Hay datos que corregir"
                };
                problem.Extensions["code"] = "VALIDATION_ERROR";
                return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
            }

            default:
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = SetPasswordWithTokenUseCase.InvalidTokenMessage
                };
                problem.Extensions["code"] = "INVALID_OR_EXPIRED_TOKEN";
                return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
            }
        }
    }
}