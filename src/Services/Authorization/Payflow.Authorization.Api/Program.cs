using Payflow.Authorization.Api.Endpoints;
using Payflow.Authorization.Application;
using Payflow.Authorization.Infrastructure;
using Payflow.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationApplication();
builder.Services.AddAuthorizationInfrastructure();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapAuthorizationEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
