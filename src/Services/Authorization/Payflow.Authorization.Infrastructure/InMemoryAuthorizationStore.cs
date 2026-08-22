using System.Collections.Concurrent;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.Infrastructure;

/// <summary>
/// Phase-1 simplification: Authorization has no database of its own, so idempotent-receiver state
/// lives in process memory. This is intentionally NOT production-safe — it doesn't survive a
/// restart and doesn't work across multiple replicas behind a load balancer. Phase 2 replaces this
/// with saga-owned state persisted alongside the orchestrator, which is the actual fix; this class
/// exists so Phase 1's demo is still correct for a single instance, not to paper over the gap.
/// </summary>
public sealed class InMemoryAuthorizationStore : IAuthorizationStore
{
    private readonly ConcurrentDictionary<Guid, AuthorizationAttempt> _byPaymentId = new();

    public Task<AuthorizationAttempt?> FindByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        Task.FromResult(_byPaymentId.GetValueOrDefault(paymentId));

    public Task SaveAsync(AuthorizationAttempt attempt, CancellationToken cancellationToken)
    {
        _byPaymentId[attempt.PaymentId] = attempt;
        return Task.CompletedTask;
    }
}
