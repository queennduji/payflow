using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Ledger.Api.Consumers;
using Payflow.Ledger.Api.Endpoints;
using Payflow.Ledger.Application;
using Payflow.Ledger.Infrastructure;
using Payflow.Ledger.Infrastructure.Persistence;
using Payflow.Shared.Api;
using Payflow.Shared.Api.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddPayflowObservability("ledger-api");

builder.Services.AddLedgerApplication();
builder.Services.AddLedgerInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PostLedgerEntryConsumer>();

    x.AddEntityFrameworkOutbox<LedgerDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
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
    .AddNpgSql(builder.Configuration.GetConnectionString("LedgerDb")!, name: "ledger-db");

var app = builder.Build();

app.UsePayflowObservability();
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
