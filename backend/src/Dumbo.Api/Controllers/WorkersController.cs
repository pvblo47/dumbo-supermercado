using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

/// <summary>
/// Worker account management endpoints restricted exclusively to <c>Admin</c> callers;
/// <c>Worker</c> callers receive 403. Scoped solely to <see cref="UserRole.Worker"/>
/// accounts so <c>Admin</c> accounts cannot be created, updated, or deleted through the API.
/// </summary>
[Route("api/workers")]
[Authorize(Roles = "Admin")]
public sealed class WorkersController : ManagedAccountsControllerBase
{
    protected override UserRole ManagedRole => UserRole.Worker;
    protected override string NotFoundTitle => "Trabajador no encontrado";
    protected override string NotFoundCode => "WORKER_NOT_FOUND";
}