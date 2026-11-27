using Dumbo.Application.EmailOutbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

/// <summary>
/// Manages failed activation and password recovery outbox emails (D06). Restricted exclusively to
/// <c>Admin</c> callers; <c>Worker</c> or <c>Client</c> roles receive 403 and unauthenticated
/// requests receive 401. Contract specification: <c>docs/CONTRATOS_CUENTAS_Y_CLIENTES.md</c>.
/// </summary>
[ApiController]
[Route("api/admin/email-outbox")]
[Authorize(Roles = "Admin")]
public sealed class AdminEmailOutboxController : ControllerBase
{
    /// <summary>
    /// Returns the 100 most recent failed outbox emails
    /// </summary>
    [HttpGet("failed")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListFailed(
        [FromServices] ListFailedEmailsUseCase useCase, CancellationToken cancellationToken) =>
        Ok(new { items = await useCase.ExecuteAsync(cancellationToken) });

    /// <summary>
    /// Requeues a failed outbox email for background delivery, returning HTTP 202 Accepted
    /// </summary>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(
        Guid id, [FromServices] RetryFailedEmailUseCase useCase, CancellationToken cancellationToken)
    {
        var outcome = await useCase.ExecuteAsync(id, cancellationToken);

        return outcome switch
        {
            RetryFailedEmailOutcome.Requeued => StatusCode(StatusCodes.Status202Accepted,
                new { message = "El correo volverá a intentarse en unos segundos." }),
            RetryFailedEmailOutcome.NotFailed =>
                Failure(StatusCodes.Status409Conflict, "El correo no está en estado fallido", "EMAIL_NOT_FAILED"),
            _ => Failure(StatusCodes.Status404NotFound, "Correo no encontrado", "EMAIL_NOT_FOUND")
        };
    }

    // Standardized problem payload with human-readable title and machine-readable code for clients.
    private static ObjectResult Failure(int status, string title, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status };
    }
}