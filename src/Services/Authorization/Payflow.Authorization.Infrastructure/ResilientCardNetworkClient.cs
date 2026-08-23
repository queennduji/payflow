using Payflow.Authorization.Application.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace Payflow.Authorization.Infrastructure;

/// <summary>
/// Decorates <see cref="SimulatedCardNetworkClient"/> with the resilience policies a real
/// integration to an external card network would need: a timeout so one slow call can't hang the
/// saga, a few quick retries for a single transient blip, and a circuit breaker so a sustained
/// outage gets a fast, honest "processor unavailable" decline instead of every payment paying the
/// full retry+timeout cost while hammering a network that's already down.
/// </summary>
/// <remarks>
/// The handler that calls this only sees <see cref="ICardNetworkClient"/> — it has no idea whether
/// the network is healthy, retrying, or tripped. Exhausting retries or finding the circuit open is
/// reported as an ordinary decline (<c>processor_unavailable</c>), not an exception: from the
/// saga's perspective this is a normal business outcome, not a fault to compensate around.
/// </remarks>
public sealed class ResilientCardNetworkClient(SimulatedCardNetworkClient inner, ResiliencePipelineProvider<string> pipelines) : ICardNetworkClient
{
    public const string PipelineKey = "card-network";

    public async Task<CardNetworkResult> AuthorizeAsync(decimal amount, string currency, string paymentMethodRef, CancellationToken cancellationToken)
    {
        var pipeline = pipelines.GetPipeline(PipelineKey);

        try
        {
            return await pipeline.ExecuteAsync(
                ct => new ValueTask<CardNetworkResult>(inner.AuthorizeAsync(amount, currency, paymentMethodRef, ct)),
                cancellationToken);
        }
        catch (CardNetworkUnavailableException)
        {
            return new CardNetworkResult(false, "processor_unavailable");
        }
        catch (BrokenCircuitException)
        {
            return new CardNetworkResult(false, "processor_unavailable");
        }
        catch (TimeoutRejectedException)
        {
            return new CardNetworkResult(false, "processor_timeout");
        }
    }
}
