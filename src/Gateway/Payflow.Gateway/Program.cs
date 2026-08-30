using Payflow.Shared.Api.Authentication;
using Payflow.Shared.Api.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddPayflowObservability("gateway");
builder.AddPayflowAuthentication();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UsePayflowObservability();
app.UsePayflowAuthentication();
app.MapHealthChecks("/health");
app.MapReverseProxy().RequireAuthorization();

app.Run();

public partial class Program;
