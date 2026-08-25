using System.Diagnostics.Metrics;

namespace Payflow.Payments.Application.Observability;

/// <summary>
/// The saga's one business-facing metric: how many payments reached each terminal status.
/// Registered as a singleton and incremented from <c>PaymentOutcomeReadyConsumer</c>, which already
/// sees every terminal outcome exactly once.
/// </summary>
public sealed class PaymentMetrics : IDisposable
{
    // Must match the meter name Payflow.Shared.Api's ObservabilityExtensions subscribes to.
    public const string MeterName = "Payflow.Payments";

    private readonly Meter _meter;
    private readonly Counter<long> _processed;

    public PaymentMetrics()
    {
        _meter = new Meter(MeterName);
        _processed = _meter.CreateCounter<long>("payflow.payments.processed", description: "Payments that reached a terminal status.");
    }

    public void RecordProcessed(string status) => _processed.Add(1, new KeyValuePair<string, object?>("status", status));

    public void Dispose() => _meter.Dispose();
}
