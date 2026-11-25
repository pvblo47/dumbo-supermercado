using Dumbo.Api.Contracts;
using Dumbo.Application.Accounts;
using Dumbo.Application.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

/// <summary>
/// Staff client administration endpoints (RF-002 manual provisioning, RF-007 management).
/// Requires <c>Worker</c> or <c>Admin</c> authorization (401 unauthenticated, 403 for clients)
/// enforced at the API boundary (NF02); contract details live in <c>docs/CONTRATOS_CUENTAS_Y_CLIENTES.md</c>.
/// </summary>
[ApiController]
[Route("api/clients")]
[Authorize(Roles = "Worker,Admin")]
public sealed class ClientsController : ControllerBase
{
    private const string ValidationTitle = "Hay datos que corregir";

    /// <summary>
    /// Creates a pending client account and enqueues an activation email
    /// </summary>
    [HttpPost]
    [ProducesResponseType<AccountSummary>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] ClientDataRequest request,
        [FromServices] CreateClientByStaffUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new CreateClientByStaffCommand(request.Email, request.FirstName, request.LastName, request.Phone),
            cancellationToken);

        return result.Outcome switch
        {
            RegisterClientOutcome.Created =>
                CreatedAtAction(nameof(GetById), new { id = result.Account!.Id }, result.Account),
            RegisterClientOutcome.EmailTaken =>
                Failure(StatusCodes.Status409Conflict, RegisterClientUseCase.EmailTakenMessage,
                    "EMAIL_ALREADY_REGISTERED", result.Errors),
            _ =>
                Failure(StatusCodes.Status400BadRequest, ValidationTitle, "VALIDATION_ERROR", result.Errors)
        };
    }

    /// <summary>
    /// Returns a paged list of non-deleted clients with optional search and status filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ClientPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] ListClientsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(new ListClientsCommand(search, status, page, pageSize), cancellationToken);

        return result.Errors is not null
            ? Failure(StatusCodes.Status400BadRequest, ValidationTitle, "VALIDATION_ERROR", result.Errors)
            : Ok(result.Page);
    }

    /// <summary>
    /// Retrieves a single client summary by ID for edit form prefill
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AccountSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id, [FromServices] GetClientUseCase useCase, CancellationToken cancellationToken)
    {
        var account = await useCase.ExecuteAsync(id, cancellationToken);
        return account is null ? ClientNotFound() : Ok(account);
    }

    /// <summary>
    /// Updates a client's first name, last name, phone, and email
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<AccountSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ClientDataRequest request,
        [FromServices] UpdateClientUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new UpdateClientCommand(id, request.Email, request.FirstName, request.LastName, request.Phone),
            cancellationToken);

        return result.Outcome switch
        {
            UpdateClientOutcome.Updated => Ok(result.Account),
            UpdateClientOutcome.NotFound => ClientNotFound(),
            UpdateClientOutcome.EmailTaken =>
                Failure(StatusCodes.Status409Conflict, RegisterClientUseCase.EmailTakenMessage,
                    "EMAIL_ALREADY_REGISTERED", result.Errors),
            _ =>
                Failure(StatusCodes.Status400BadRequest, ValidationTitle, "VALIDATION_ERROR", result.Errors)
        };
    }

    /// <summary>
    /// Queues a new activation link email for a pending client and returns <c>202 Accepted</c>;
    /// prior tokens expire once the replacement link is issued in the background.
    /// </summary>
    [HttpPost("{id:guid}/activation-link")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendActivationLink(
        Guid id, [FromServices] ResendActivationLinkUseCase useCase, CancellationToken cancellationToken)
    {
        var outcome = await useCase.ExecuteAsync(id, cancellationToken);

        return outcome switch
        {
            ResendActivationOutcome.Queued => StatusCode(StatusCodes.Status202Accepted,
                new { message = "Se enviará un nuevo enlace de activación al correo del cliente." }),
            ResendActivationOutcome.NotPending =>
                Failure(StatusCodes.Status409Conflict, "La cuenta no está pendiente de activación",
                    "ACCOUNT_NOT_PENDING"),
            _ => ClientNotFound()
        };
    }

    /// <summary>
    /// Disables a client account, revoking access immediately; idempotent
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType<AccountSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Disable(
        Guid id, [FromServices] ChangeClientStatusUseCase useCase, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, ClientStatusAction.Disable, useCase, cancellationToken);

    /// <summary>
    /// Re-enables a disabled client account; idempotent
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType<AccountSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Enable(
        Guid id, [FromServices] ChangeClientStatusUseCase useCase, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, ClientStatusAction.Enable, useCase, cancellationToken);

    private async Task<IActionResult> ChangeStatusAsync(
        Guid id, ClientStatusAction action, ChangeClientStatusUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, action, cancellationToken);
        return result.Outcome == ChangeClientStatusOutcome.Changed ? Ok(result.Account) : ClientNotFound();
    }

    private static ObjectResult ClientNotFound() =>
        Failure(StatusCodes.Status404NotFound, "Cliente no encontrado", "CLIENT_NOT_FOUND");

    // Builds uniform ProblemDetails responses with a machine-readable "code" extension and optional field errors.
    private static ObjectResult Failure(
        int status, string title, string code, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        ProblemDetails problem = errors is null
            ? new ProblemDetails { Status = status, Title = title }
            : new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = status,
                Title = title
            };
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status };
    }
}