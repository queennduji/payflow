using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Authorization.Api.Consumers;
using Payflow.Authorization.Api.Endpoints;
using Payflow.Authorization.Application;
using Payflow.Authorization.Infrastructure;
using Payflow.Authorization.Infrastructure.Persistence;
using Payflow.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationApplication();
builder.Services.AddAuthorizationInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AuthorizePaymentConsumer>();
    x.AddConsumer<VoidAuthorizationConsumer>();

    x.AddEntityFrameworkOutbox<AuthorizationDbContext>(o =>
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
    .AddNpgSql(builder.Configuration.GetConnectionString("AuthorizationDb")!, name: "authorization-db");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapAuthorizationEndpoints();
app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthorizationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
