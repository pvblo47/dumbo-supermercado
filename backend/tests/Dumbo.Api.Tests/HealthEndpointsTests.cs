using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Dumbo.Api.Tests;

public sealed class HealthEndpointsTests
{
    private static WebApplicationFactory<Program> CreateFactory(string connection) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Dumbo"] = connection
                }));
        });

    private const string UnavailableDatabase =
        "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Timeout=1;Command Timeout=1;Pooling=false";

    [Fact]
    public async Task Liveness_does_not_require_a_database()
    {
        await using var factory = CreateFactory(UnavailableDatabase);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("ok", body!["status"]);
        Assert.Equal("dumbo-api", body["service"]);
    }

    [Fact]
    public async Task Readiness_returns_controlled_problem_when_database_is_down()
    {
        await using var factory = CreateFactory(UnavailableDatabase);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("DATABASE_UNAVAILABLE", body);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Development_exposes_the_openapi_contract()
    {
        await using var factory = CreateFactory(UnavailableDatabase);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/health/ready", await response.Content.ReadAsStringAsync());
    }

    [PostgresFact]
    public async Task Readiness_accepts_a_real_postgres_connection()
    {
        await using var factory = CreateFactory(Environment.GetEnvironmentVariable("DUMBO_TEST_DATABASE")!);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ready", await response.Content.ReadAsStringAsync());
    }
}

internal sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DUMBO_TEST_DATABASE")))
            Skip = "Define DUMBO_TEST_DATABASE para ejecutar la prueba con PostgreSQL real.";
    }
}
