using Dumbo.Application.Abstractions;
using Dumbo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dumbo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DumboDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("Dumbo")
            ?? "Host=localhost;Port=5432;Database=dumbo;Username=dumbo;Timeout=3;Command Timeout=5"));
        services.AddScoped<IDatabaseReadinessChecker, DatabaseReadinessChecker>();
        return services;
    }
}
