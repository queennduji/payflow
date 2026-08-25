using Payflow.Shared.Api.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddPayflowObservability("gateway");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UsePayflowObservability();
app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();

public partial class Program;
