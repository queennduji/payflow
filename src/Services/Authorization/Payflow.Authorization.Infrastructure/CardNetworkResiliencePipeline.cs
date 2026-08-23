using Payflow.Authorization.Application.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Payflow.Authorization.Infrastructure;

/// <summary>
/// The resilience policy protecting calls to the (simulated) card network: timeout so one slow
/// call can't hang the saga, a few quick exponential-backoff retries for a single blip, then a
/// circuit breaker so a sustained outage fails fast instead of every payment paying the full
/// retry+timeout cost. Factored out of DI registration so the policy shape itself is unit-testable
/// without executing it (see <c>CardNetworkResiliencePipelineTests</c>) — asserting "3 retries,
/// exponential backoff, breaks at a 50% failure ratio" shouldn't require real delays.
/// </summary>
public static class CardNetworkResiliencePipeline
{
    public static void Configure(ResiliencePipelineBuilder builder)
    {
        builder
            .AddTimeout(TimeSpan.FromSeconds(2))
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<CardNetworkUnavailableException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(100)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<CardNetworkUnavailableException>(),
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = TimeSpan.FromSeconds(15)
            });
    }
}
