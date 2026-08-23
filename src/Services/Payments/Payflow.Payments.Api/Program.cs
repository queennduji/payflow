using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Payments.Api.Consumers;
using Payflow.Payments.Api.Endpoints;
using Payflow.Payments.Application;
using Payflow.Payments.Application.Saga;
using Payflow.Payments.Infrastructure;
using Payflow.Payments.Infrastructure.Persistence;
using Payflow.Shared.Api;
using Payflow.Shared.Contracts.Messages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPaymentsApplication();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<PaymentSagaStateMachine, PaymentSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<PaymentsDbContext>();
            r.UsePostgres();
        });

    // See ADR-0006: this consumer — not the saga directly — sends the response to a pending
    // IRequestClient<ProcessPayment> await, so that send only ever happens after the saga's
    // outcome has actually committed.
    x.AddConsumer<PaymentOutcomeReadyConsumer>();

    // Payments.Api's own synchronous facade over the saga — see ADR-0006.
    x.AddRequestClient<ProcessPayment>(TimeSpan.FromSeconds(10));

    x.AddEntityFrameworkOutbox<PaymentsDbContext>(o =>
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

        // Postgres SERIALIZABLE isolation (used by the EF saga/outbox integration) can
        // legitimately reject a transaction with a 40001 conflict under concurrent load — this
        // is expected, retriable behavior, not a bug; without a retry policy those faults would
        // otherwise strand messages (and, for the saga, HTTP callers) on the first collision.
        cfg.UseMessageRetry(r => r.Intervals(50, 100, 200, 500, 1000));
        cfg.ConfigureEndpoints(context);
    });
});

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
