using System.Security.Claims;
using Dumbo.Api.Contracts;
using Dumbo.Application.Accounts;
using Dumbo.Application.Clients;
using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

/// <summary>
/// Shared controller actions for managing <see cref="UserRole.Client"/> and
/// <see cref="UserRole.Worker"/> accounts under identical lifecycle rules, differing only
/// by target role, role authorization attributes on derived controllers, and domain messages.
/// </summary>
[ApiController]
public abstract class ManagedAccountsControllerBase : ControllerBase
{
    private const string ValidationTitle = "Hay datos que corregir";

    /// <summary>
    /// Target account role managed by the derived controller
    /// </summary>
    protected abstract UserRole ManagedRole { get; }
    protected abstract string NotFoundTitle { get; }
    protected abstract string NotFoundCode { get; }

    /// <summary>
    /// Manually provisions a pending account and queues an activation email
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ClientDataRequest request,
        [FromServices] CreateClientByStaffUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new CreateClientByStaffCommand(request.Email, request.FirstName, request.LastName, request.Phone, ManagedRole),
            cancellationToken);

        return result.Outcome switch
        {
            RegisterClientOutcome.Created =>
                CreatedAtAction(nameof(GetById), new { id = result.Account!.Id }, result.Account),
            RegisterClientOutcome.EmailTaken =>
                Failure(StatusCodes.Status409Conflict, RegisterClientUseCase.EmailTakenMessage,
                    "EMAIL_ALREADY_REGISTERED", result.Errors),
            _ => Failure(StatusCodes.Status400BadRequest, ValidationTitle, "VALIDATION_ERROR", result.Errors)
        };
    }

    /// <summary>
    /// Returns a paged list of non-deleted accounts filtered by search text and status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int? page, [FromQuery] int? pageSize,
        [FromServices] ListClientsUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new ListClientsCommand(search, status, page, pageSize, ManagedRole), cancellationToken);
        return result.Errors is not null
            ? Failure(StatusCodes.Status400BadRequest, ValidationTitle, "VALIDATION_ERROR", result.Errors)
            : Ok(result.Page);
    }

    /// <summary>
    /// Returns a separate paged view of soft-deleted accounts
    /// </summary>
    [HttpGet("deleted")]
    public async Task<IActionResult> ListDeleted(
        [FromQuery] string? search, [FromQuery] int? page, [FromQuery] int? pageSize,
        [FromServices] ListClientsUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new ListClientsCommand(search, null, page, pageSize, ManagedRole, Deleted: true), cancellationToken);
        return Ok(result.Page);
    }

    /// <summary>
    /// Retrieves a single account by ID to populate edit forms
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id, [FromServices] GetClientUseCase useCase, CancellationToken cancellationToken)
    {
        var account = await useCase.ExecuteAsync(id, cancellationToken, ManagedRole);
        return account is null ? NotFoundResponse() : Ok(account);
    }

    /// <summary>
    /// Updates first name, last name, phone, and email for an existing account
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] ClientDataRequest request,
        [FromServices] UpdateClientUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new UpdateClientCommand(id, request.Email, request.FirstName, request.LastName, request.Phone, ManagedRole),
            cancellationToken);

        return result.Outcome switch
        {
            UpdateClientOutcome.Updated => Ok(result.Account),
            UpdateClientOutcome.NotFound => NotFoundResponse(),
            UpdateClientOutcome.EmailTaken =>
                Failure(StatusCodes.Status409Conflict, RegisterClientUseCase.EmailTakenMessage,
                    "EMAIL_ALREADY_REGISTERED", result.Errors),
            _ => Failure(StatusCodes.Status400BadRequest, ValidationTitle, "VALIDATION_ERROR", result.Errors)
        };
    }

    /// <summary>
    /// Queues a replacement activation link for a pending account, returning HTTP 202 Accepted
    /// </summary>
    [HttpPost("{id:guid}/activation-link")]
    public async Task<IActionResult> ResendActivationLink(
        Guid id, [FromServices] ResendActivationLinkUseCase useCase, CancellationToken cancellationToken)
    {
        var outcome = await useCase.ExecuteAsync(id, cancellationToken, ManagedRole);
        return outcome switch
        {
            ResendActivationOutcome.Queued => StatusCode(StatusCodes.Status202Accepted,
                new { message = "Se enviará un nuevo enlace de activación al correo de la cuenta." }),
            ResendActivationOutcome.NotPending =>
                Failure(StatusCodes.Status409Conflict, "La cuenta no está pendiente de activación", "ACCOUNT_NOT_PENDING"),
            _ => NotFoundResponse()
        };
    }

    /// <summary>
    /// Idempotently disables an account, revoking active access immediately
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    public Task<IActionResult> Disable(
        Guid id, [FromServices] ChangeClientStatusUseCase useCase, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, ClientStatusAction.Disable, useCase, cancellationToken);

    /// <summary>
    /// Idempotently enables a disabled account according to activation rules
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    public Task<IActionResult> Enable(
        Guid id, [FromServices] ChangeClientStatusUseCase useCase, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, ClientStatusAction.Enable, useCase, cancellationToken);

    /// <summary>
    /// Idempotently soft-deletes an account and records an audit entry using the caller's token subject
    /// </summary>
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(
        Guid id, [FromServices] ChangeAccountLifecycleUseCase useCase, CancellationToken cancellationToken) =>
        LifecycleAsync(id, AccountLifecycleAction.Delete, useCase, cancellationToken);

    /// <summary>
    /// Restores a soft-deleted account to Disabled status, or returns 409 if not deleted
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    public Task<IActionResult> Restore(
        Guid id, [FromServices] ChangeAccountLifecycleUseCase useCase, CancellationToken cancellationToken) =>
        LifecycleAsync(id, AccountLifecycleAction.Restore, useCase, cancellationToken);

    private async Task<IActionResult> ChangeStatusAsync(
        Guid id, ClientStatusAction action, ChangeClientStatusUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, action, cancellationToken, ManagedRole);
        return result.Outcome == ChangeClientStatusOutcome.Changed ? Ok(result.Account) : NotFoundResponse();
    }

    private async Task<IActionResult> LifecycleAsync(
        Guid id, AccountLifecycleAction action, ChangeAccountLifecycleUseCase useCase,
        CancellationToken cancellationToken)
    {
        // Derive the acting user ID strictly from the authenticated JWT "sub" claim rather than request payloads.
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var actorId))
            return Unauthorized();

        var result = await useCase.ExecuteAsync(id, actorId, action, ManagedRole, cancellationToken);
        return result.Outcome switch
        {
            AccountLifecycleOutcome.Changed => Ok(result.Account),
            AccountLifecycleOutcome.NotDeleted =>
                Failure(StatusCodes.Status409Conflict, "La cuenta no está eliminada", "ACCOUNT_NOT_DELETED"),
            _ => NotFoundResponse()
        };
    }

    private ObjectResult NotFoundResponse() => Failure(StatusCodes.Status404NotFound, NotFoundTitle, NotFoundCode);

    // Standardized problem payload with human-readable title, machine-readable code for clients, and optional field errors.
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