using Microsoft.EntityFrameworkCore;
using Payflow.Payments.Api.Endpoints;
using Payflow.Payments.Application;
using Payflow.Payments.Infrastructure;
using Payflow.Payments.Infrastructure.Persistence;
using Payflow.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPaymentsApplication();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PaymentsDb")!, name: "payments-db");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPaymentEndpoints();
app.MapHealthChecks("/health");

// Demo/dev convenience only: applies pending EF Core migrations on boot so `docker compose up`
// is a one-command demo. A real deployment runs migrations as a separate release step, not
// racing multiple instances of the same service against each other on startup.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program; // exposed for WebApplicationFactory-based integration tests (Phase 7)
