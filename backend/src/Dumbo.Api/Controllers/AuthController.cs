using Dumbo.Api.Contracts;
using Dumbo.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

/// <summary>
/// Manages authentication endpoints such as client self-registration and credential handling.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Registers a new client account in the system.
    /// </summary>
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
    /// Helper method to build standardized RFC 7807 problem details responses with custom error extensions.
    /// </summary>
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