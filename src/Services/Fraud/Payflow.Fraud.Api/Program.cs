using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Fraud.Api.Consumers;
using Payflow.Fraud.Api.Endpoints;
using Payflow.Fraud.Application;
using Payflow.Fraud.Infrastructure;
using Payflow.Fraud.Infrastructure.Persistence;
using Payflow.Shared.Api;
using Payflow.Shared.Api.Authentication;
using Payflow.Shared.Api.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddPayflowObservability("fraud-api");
builder.AddPayflowAuthentication();

builder.Services.AddFraudApplication();
builder.Services.AddFraudInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CheckFraudConsumer>();

    x.AddEntityFrameworkOutbox<FraudDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", builder.Configuration.GetValue("RabbitMq:Port", (ushort)5672), "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        // Postgres SERIALIZABLE isolation (used by the EF outbox integration) can legitimately
        // reject a transaction with a 40001 conflict under concurrent load — expected, retriable
        // behavior, not a bug; without a retry policy those faults would strand messages.
        cfg.UseMessageRetry(r => r.Intervals(50, 100, 200, 500, 1000));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("FraudDb")!, name: "fraud-db");

var app = builder.Build();

app.UsePayflowObservability();
app.UsePayflowAuthentication();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapFraudEndpoints();
app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FraudDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
