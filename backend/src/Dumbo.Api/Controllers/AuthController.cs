using System.Security.Claims;
using Dumbo.Api.Contracts;
using Dumbo.Application.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

/// <summary>
/// Accounts and access: registration, sign-in, and current session lookup.
/// Controllers remain thin by translating HTTP requests into Application use cases and back.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Self-service Client registration (RF-002). The assigned role is always Client.</summary>
    [HttpPost("register")]
    [ProducesResponseType<AccountSummary>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] RegisterClientUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new RegisterClientCommand(
                request.Email, request.FirstName, request.LastName, request.Phone, request.Password),
            cancellationToken);

        return result.Outcome switch
        {
            RegisterClientOutcome.Created =>
                StatusCode(StatusCodes.Status201Created, result.Account),
            RegisterClientOutcome.EmailTaken =>
                ProblemWithErrors(StatusCodes.Status409Conflict, "El correo ya está registrado",
                    "EMAIL_ALREADY_REGISTERED", result.Errors!),
            _ =>
                ProblemWithErrors(StatusCodes.Status400BadRequest, "Hay datos que corregir",
                    "VALIDATION_ERROR", result.Errors!)
        };
    }

    /// <summary>
    /// User sign-in (RF-001). On success, returns a JWT token that Angular must send
    /// on subsequent requests as: Authorization: Bearer (token).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthSession>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new LoginCommand(request.Email, request.Password), cancellationToken);

        return result.Outcome switch
        {
            LoginOutcome.Success => Ok(result.Session),
            LoginOutcome.Disabled =>
                LoginProblem(StatusCodes.Status403Forbidden, result.Message!, "ACCOUNT_DISABLED"),
            _ =>
                LoginProblem(StatusCodes.Status401Unauthorized, result.Message!, "INVALID_CREDENTIALS")
        };
    }

    /// <summary>
    /// Retrieves the authenticated account profile so Angular can verify token validity
    /// and render the role-appropriate interface.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<AccountSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(
        [FromServices] GetCurrentAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var accountId))
            return Unauthorized();

        var account = await useCase.ExecuteAsync(accountId, cancellationToken);
        return account is null ? Unauthorized() : Ok(account);
    }

    /// <summary>
    /// TEMPORARY endpoint to verify role authorization: 401 without a token, 403 for Client tokens,
    /// and 200 for Worker or Admin tokens. Remove once real role-scoped endpoints exist.
    /// </summary>
    [HttpGet("staff-check")]
    [Authorize(Roles = "Worker,Admin")]
    public IActionResult StaffCheck() => Ok(new { message = "Acceso permitido para personal de la tienda" });

    // Sign-in error response: places the exact SRS message in "title" and the error code in "code".
    private static ObjectResult LoginProblem(int status, string message, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = message };
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status };
    }

    private static ObjectResult ProblemWithErrors(
        int status, string title, string code, IReadOnlyDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value))
        {
            Status = status,
            Title = title
        };
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status };
    }
}