using Dumbo.Api.Extensions;
using Dumbo.Application.Accounts;
using Dumbo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Service registration (all "builder.Services.Add..." calls must occur BEFORE Build())
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Infrastructure layer: PostgreSQL, account store and queries, password hasher, and JWT issuer.
builder.Services.AddInfrastructure(builder.Configuration);

// JWT token validation and role-based authorization (RF-001, NF02).
builder.Services.AddDumboJwtAuthentication();

// Application layer use cases (scoped per HTTP request).
builder.Services.AddScoped<RegisterClientUseCase>();        // RF-002 self-service registration
builder.Services.AddScoped<LoginUseCase>();                 // RF-001 sign-in
builder.Services.AddScoped<GetCurrentAccountUseCase>();     // current session account


// HTTP pipeline (all "app.Use..." / "app.Map..." calls must occur AFTER Build())
var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Order matters: authenticate the caller first, then authorize what they can access.
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

// Exposes the entry point for integration tests (WebApplicationFactory<Program>).
public partial class Program;