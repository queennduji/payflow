using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Notifications.Api.Consumers;
using Payflow.Notifications.Api.Endpoints;
using Payflow.Notifications.Application;
using Payflow.Notifications.Infrastructure;
using Payflow.Notifications.Infrastructure.Persistence;
using Payflow.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotificationsApplication();
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendPaymentNotificationConsumer>();

    x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
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
    .AddNpgSql(builder.Configuration.GetConnectionString("NotificationsDb")!, name: "notifications-db");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapNotificationEndpoints();
app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program;
