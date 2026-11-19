using Dumbo.Application.Abstractions;
using Dumbo.Application.Email;
using Dumbo.Application.Security;
using Dumbo.Infrastructure.Email;
using Dumbo.Infrastructure.Persistence;
using Dumbo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dumbo.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services in the dependency injection container.
/// The API layer calls only <see cref="AddInfrastructure"/> so controllers and use cases depend
/// strictly on Application abstractions rather than EF Core or email transport details.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers database persistence, account/security services, outbox stores, and email senders.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL connection string ("ConnectionStrings:Dumbo").
        services.AddDbContext<DumboDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("Dumbo")
            ?? "Host=localhost;Port=5432;Database=dumbo;Username=dumbo;Timeout=3;Command Timeout=5"));

        services.AddScoped<IDatabaseReadinessChecker, DatabaseReadinessChecker>();

        //Accounts
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IAccountPasswordHasher, IdentityAccountPasswordHasher>();
        services.AddScoped<IAccountStore, AccountStore>();

        //JWT sign-in
        services.AddScoped<IAccountLookup, AccountLookup>();
        services.AddSingleton<JwtSettings>(sp => JwtSettingsFactory.Create(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IHostEnvironment>()));
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        //Activation, recovery, and email outbox
        services.AddScoped<IActivationTokenStore, ActivationTokenStore>();
        services.AddScoped<IEmailOutboxStore, EmailOutboxStore>();

        // Public frontend base URL used to build email action links.
        services.AddSingleton(sp => new EmailLinkSettings(
            sp.GetRequiredService<IConfiguration>()["App:PublicBaseUrl"] ?? "http://localhost:8080"));

        services.AddHttpClient<ResendEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Uses Resend when "Resend:ApiKey" is set; falls back to local files only in Development.
        // Missing the key outside Development fails fast as a deployment configuration error.
        services.AddScoped<IEmailSender>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var environment = sp.GetRequiredService<IHostEnvironment>();

            if (!string.IsNullOrWhiteSpace(config["Resend:ApiKey"]))
                return sp.GetRequiredService<ResendEmailSender>();
            if (environment.IsDevelopment())
                return new FileEmailSender(environment);

            throw new InvalidOperationException(
                "Falta la configuración 'Resend:ApiKey' (variable de entorno Resend__ApiKey) para enviar correos.");
        });

        return services;
    }
}