using Dumbo.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ServiceHealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<ServiceHealthResponse> Get() =>
        Ok(new ServiceHealthResponse("ok", "dumbo-api", "0.1.0"));

    [HttpGet("ready")]
    [ProducesResponseType<ReadinessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReadinessResponse>> GetReadiness(
        [FromServices] IDatabaseReadinessChecker checker, CancellationToken cancellationToken)
    {
        if (await checker.CanConnectAsync(cancellationToken))
            return Ok(new ReadinessResponse("ready", new DatabaseCheck("up")));

        return Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Base de datos no disponible",
            detail: "Comprueba el servicio PostgreSQL y la configuración de conexión.",
            extensions: new Dictionary<string, object?> { ["code"] = "DATABASE_UNAVAILABLE" });
    }
}

public sealed record ServiceHealthResponse(string Status, string Service, string Version);
public sealed record DatabaseCheck(string Database);
public sealed record ReadinessResponse(string Status, DatabaseCheck Checks);
