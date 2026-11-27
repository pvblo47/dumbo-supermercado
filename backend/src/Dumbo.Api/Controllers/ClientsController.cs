using Dumbo.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

/// <summary>
/// Client account management endpoints restricted to
/// <c>Worker</c> and <c>Admin</c> roles. Returns 401 when unauthenticated and 403 for
/// <c>Client</c> callers. Contract specification: <c>docs/CONTRATOS_CUENTAS_Y_CLIENTES.md</c>.
/// </summary>
[Route("api/clients")]
[Authorize(Roles = "Worker,Admin")]
public sealed class ClientsController : ManagedAccountsControllerBase
{
    protected override UserRole ManagedRole => UserRole.Client;
    protected override string NotFoundTitle => "Cliente no encontrado";
    protected override string NotFoundCode => "CLIENT_NOT_FOUND";
}