using Payflow.Payments.Domain;

namespace Payflow.Payments.Application.Abstractions;

/// <summary>
/// Persistence port for idempotency dedup records. <see cref="SaveAsync"/> only stages the insert;
/// the actual dedup guarantee comes from a unique index on (MerchantId, Key) enforced when the unit
/// of work commits. Two concurrent requests with the same key can both pass <see cref="FindAsync"/>
/// and both call <see cref="SaveAsync"/> – the loser's commit fails with a constraint violation,
/// which the caller (see SubmitPaymentCommandHandler) turns into a re-fetch-and-replay instead of a 500.
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(string merchantId, string key, CancellationToken cancellationToken);
    Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken);
}
