using FluentAssertions;
using Payflow.Authorization.Infrastructure;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Testing;

namespace Payflow.Authorization.UnitTests;

/// <summary>
/// Asserts the resilience policy's *shape* – retry count, backoff, circuit-breaker thresholds –
/// via Polly's pipeline descriptor rather than by executing it, so these tests run in milliseconds
/// instead of paying for real timeouts/backoff delays.
/// </summary>
public class CardNetworkResiliencePipelineTests
{
    private static ResiliencePipelineDescriptor BuildDescriptor()
    {
        var builder = new ResiliencePipelineBuilder();
        CardNetworkResiliencePipeline.Configure(builder);
        return builder.Build().GetPipelineDescriptor();
    }

    [Fact]
    public void Retries_a_few_times_with_exponential_backoff()
    {
        var retry = BuildDescriptor().Strategies
            .Select(s => s.Options)
            .OfType<RetryStrategyOptions>()
            .Single();

        retry.MaxRetryAttempts.Should().Be(3);
        retry.BackoffType.Should().Be(DelayBackoffType.Exponential);
    }

    [Fact]
    public void Breaks_the_circuit_at_a_fifty_percent_failure_ratio()
    {
        var breaker = BuildDescriptor().Strategies
            .Select(s => s.Options)
            .OfType<CircuitBreakerStrategyOptions>()
            .Single();

        breaker.FailureRatio.Should().Be(0.5);
        breaker.MinimumThroughput.Should().Be(5);
    }

    [Fact]
    public void Bounds_a_single_attempt_with_a_timeout()
    {
        var strategies = BuildDescriptor().Strategies;

        strategies.Should().Contain(s => s.Options is Polly.Timeout.TimeoutStrategyOptions);
    }
}
