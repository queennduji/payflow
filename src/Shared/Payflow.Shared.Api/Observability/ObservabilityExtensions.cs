using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace Payflow.Shared.Api.Observability;

/// <summary>
/// Wires logging, tracing, and metrics the same way for every service: Serilog owns logs (console
/// for local dev, OTLP to the collector so they land in Loki), the OpenTelemetry SDK owns traces
/// and metrics (also OTLP to the collector, which fans out to Tempo and Prometheus). Every log line
/// is stamped with the current trace/span ID so a span in Tempo and its log lines in Loki can be
/// correlated.
/// </summary>
public static class ObservabilityExtensions
{
    // Must match the Meter name PaymentMetrics registers in Payflow.Payments.Application. Harmless
    // to subscribe to on services that never publish it.
    private const string PaymentsMeterName = "Payflow.Payments";

    public static WebApplicationBuilder AddPayflowObservability(this WebApplicationBuilder builder, string serviceName)
    {
        var collectorUrl = builder.Configuration["Otel:CollectorUrl"] ?? "http://localhost:4317";

        builder.Host.UseSerilog((_, services, configuration) => configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.WithProperty("service.name", serviceName)
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint = collectorUrl;
                o.ResourceAttributes.Add("service.name", serviceName);
            }));

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // MassTransit and Npgsql both emit ActivitySource spans natively – registering the
                // source name is the entire integration, no extra instrumentation package needed.
                .AddSource("MassTransit")
                .AddSource("Npgsql")
                .AddOtlpExporter(o => o.Endpoint = new Uri(collectorUrl)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(PaymentsMeterName)
                .AddOtlpExporter(o => o.Endpoint = new Uri(collectorUrl)));

        return builder;
    }

    public static WebApplication UsePayflowObservability(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        return app;
    }
}
