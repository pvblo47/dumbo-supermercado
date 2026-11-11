namespace Dumbo.Application.Abstractions;

public interface IDatabaseReadinessChecker
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
