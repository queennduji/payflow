using Payflow.Payments.Domain;

namespace Payflow.Payments.Application.Abstractions;

/// <summary>Persistence port for the Payment aggregate. Implemented by Infrastructure (EF Core).</summary>
public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Payment?> GetByMerchantAndIdempotencyKeyAsync(string merchantId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// Re-reads <paramref name="payment"/>'s current state from the database, overwriting whatever
    /// this unit of work's change tracker already has cached for it. Needed whenever a tracked
    /// instance might have been mutated by a different process/scope since it was first loaded —
    /// e.g. after awaiting the saga, which updates the row from Payments' own message consumers,
    /// each running in their own scope. A plain re-query by id would otherwise silently return the
    /// stale tracked instance instead of hitting the database (EF Core's identity map).
    /// </summary>
    Task ReloadAsync(Payment payment, CancellationToken cancellationToken);
}
