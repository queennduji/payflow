namespace Payflow.Payments.Application.Abstractions;

/// <summary>
/// Thrown by <see cref="IUnitOfWork.SaveChangesAsync"/> when committing an
/// <see cref="Payflow.Payments.Domain.IdempotencyRecord"/> loses a race against a concurrent
/// request using the same (merchant, key) pair. This is the Application-layer translation of a
/// database unique-constraint violation — callers should treat it as "someone already handled
/// this request" and re-fetch the winning record, not as an unexpected error.
/// </summary>
public sealed class IdempotencyKeyConflictException : Exception
{
    public IdempotencyKeyConflictException(string merchantId, string key, Exception innerException)
        : base($"Idempotency key '{key}' for merchant '{merchantId}' was already reserved by a concurrent request.", innerException)
    {
    }
}
