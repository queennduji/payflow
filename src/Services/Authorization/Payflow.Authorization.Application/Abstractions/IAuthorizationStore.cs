using Payflow.Authorization.Domain;

namespace Payflow.Authorization.Application.Abstractions;

/// <summary>
/// Idempotent-receiver store keyed by PaymentId: Payments may retry the authorize call
/// (client timeout, transient network error), and this must return the original decision rather
/// than authorizing the same payment twice.
/// </summary>
public interface IAuthorizationStore
{
    Task<AuthorizationAttempt?> FindByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task SaveAsync(AuthorizationAttempt attempt, CancellationToken cancellationToken);
}
