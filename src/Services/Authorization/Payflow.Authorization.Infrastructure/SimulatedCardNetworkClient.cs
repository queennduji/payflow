using Microsoft.Extensions.Options;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.Infrastructure;

public sealed class ChaosOptions
{
    /// <summary>Probability (0.0–1.0) that a call throws <see cref="CardNetworkUnavailableException"/> instead of returning a decision.</summary>
    public double CardNetworkFaultRate { get; set; }
}

/// <summary>
/// Stands in for the actual network call to a card processor: adds realistic latency and,
/// deliberately, a configurable chance of failing outright — the thing a real network call can do
/// that a pure business-rule function never would. The decision itself is untouched, unmocked
/// business logic (<see cref="AuthorizationDecisionEngine"/>); only the "is the call itself healthy
/// right now" question is simulated here. Fault injection is hand-rolled rather than via Simmy,
/// which has no release compatible with Polly v8.
/// </summary>
public sealed class SimulatedCardNetworkClient(IOptions<ChaosOptions> chaosOptions) : ICardNetworkClient
{
    private static readonly Random Random = new();

    public async Task<CardNetworkResult> AuthorizeAsync(decimal amount, string currency, string paymentMethodRef, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Next(20, 150), cancellationToken);

        if (Random.NextDouble() < chaosOptions.Value.CardNetworkFaultRate)
            throw new CardNetworkUnavailableException("Simulated card network fault (chaos injection).");

        var (approved, declineReason) = AuthorizationDecisionEngine.Decide(amount, currency, paymentMethodRef);
        return new CardNetworkResult(approved, declineReason);
    }
}
