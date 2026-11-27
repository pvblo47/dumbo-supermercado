using Dumbo.Api.Extensions;
using Dumbo.Api.Workers;
using Dumbo.Application.Accounts;
using Dumbo.Infrastructure;
using Dumbo.Application.Clients;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();


builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddDumboJwtAuthentication();
builder.Services.AddScoped<RegisterClientUseCase>();  
builder.Services.AddScoped<LoginUseCase>();                   
builder.Services.AddScoped<GetCurrentAccountUseCase>();      
builder.Services.AddScoped<RequestPasswordRecoveryUseCase>();   
builder.Services.AddScoped<SetPasswordWithTokenUseCase>();     
builder.Services.AddScoped<ProcessEmailOutboxUseCase>();     
builder.Services.AddScoped<CreateClientByStaffUseCase>();
builder.Services.AddScoped<ResendActivationLinkUseCase>();
builder.Services.AddScoped<ListClientsUseCase>();
builder.Services.AddScoped<GetClientUseCase>();
builder.Services.AddScoped<UpdateClientUseCase>();
builder.Services.AddScoped<ChangeClientStatusUseCase>();   
builder.Services.AddScoped<ChangeAccountLifecycleUseCase>();

builder.Services.AddHostedService<EmailOutboxWorker>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Order matters: authenticate caller identity first, then enforce authorization rules.
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

// Exposed partial entry point for integration tests (WebApplicationFactory<Program>).
public partial class Program;