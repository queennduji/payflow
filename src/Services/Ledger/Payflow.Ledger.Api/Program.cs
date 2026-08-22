using Microsoft.EntityFrameworkCore;
using Payflow.Ledger.Api.Endpoints;
using Payflow.Ledger.Application;
using Payflow.Ledger.Infrastructure;
using Payflow.Ledger.Infrastructure.Persistence;
using Payflow.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLedgerApplication();
builder.Services.AddLedgerInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("LedgerDb")!, name: "ledger-db");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapLedgerEndpoints();
app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
