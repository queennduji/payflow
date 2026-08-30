using Microsoft.EntityFrameworkCore;
using Payflow.Shared.Api;
using Payflow.Shared.Api.Authentication;
using Payflow.Shared.Api.Observability;
using Payflow.Vault.Api.Endpoints;
using Payflow.Vault.Application;
using Payflow.Vault.Infrastructure;
using Payflow.Vault.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.AddPayflowObservability("vault-api");
builder.AddPayflowAuthentication();

builder.Services.AddVaultApplication();
builder.Services.AddVaultInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("VaultDb")!, name: "vault-db");

var app = builder.Build();

app.UsePayflowObservability();
app.UsePayflowAuthentication();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapVaultEndpoints();
app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VaultDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
