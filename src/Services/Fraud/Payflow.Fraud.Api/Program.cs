using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Fraud.Api.Consumers;
using Payflow.Fraud.Api.Endpoints;
using Payflow.Fraud.Application;
using Payflow.Fraud.Infrastructure;
using Payflow.Fraud.Infrastructure.Persistence;
using Payflow.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

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
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

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
