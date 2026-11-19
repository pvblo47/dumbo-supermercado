using Dumbo.Application.Abstractions;
using Dumbo.Infrastructure.Persistence;
using Dumbo.Infrastructure.Security; 
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
        services.AddSingleton<TimeProvider>(TimeProvider.System);                                  
        services.AddSingleton<IAccountPasswordHasher, IdentityAccountPasswordHasher>();            
        services.AddScoped<IAccountStore, AccountStore>();  
        return services;
    }
}
