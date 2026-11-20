using Dumbo.Application.Accounts;

namespace Dumbo.Api.Workers;

/// <summary>
/// Background hosted service that polls and dispatches due outbox emails every 15 seconds (D06),
/// avoiding Redis or an external broker. Can be disabled via <c>Email:WorkerEnabled = false</c> (used in tests).
/// </summary>
public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopes, IConfiguration configuration, ILogger<EmailOutboxWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 20;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Email:WorkerEnabled", true))
        {
            logger.LogInformation("Proceso de envío de correos desactivado por configuración.");
            return;
        }

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Normal API shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create a fresh DI scope per cycle so each tick uses its own DbContext instance.
            await using var scope = scopes.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ProcessEmailOutboxUseCase>();
            await useCase.ExecuteAsync(BatchSize, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Transient failures (such as a temporary database outage) should not stop the worker;
            // the next tick will retry. Never log email addresses, links, or tokens (D06).
            logger.LogError(ex, "Falló el procesamiento de la cola de correos; se reintentará en el próximo ciclo.");
        }
    }
}