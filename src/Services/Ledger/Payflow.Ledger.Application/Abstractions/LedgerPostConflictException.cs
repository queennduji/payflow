namespace Payflow.Ledger.Application.Abstractions;

/// <summary>
/// Thrown when committing a new <see cref="Payflow.Ledger.Domain.LedgerEntryGroup"/> loses a race
/// against a concurrent post for the same PaymentId (unique index in Infrastructure). Callers
/// should re-fetch the winning group and treat it as the answer, not as an error.
/// </summary>
public sealed class LedgerPostConflictException(Guid paymentId, Exception innerException)
    : Exception($"A ledger entry group for payment '{paymentId}' was already posted by a concurrent request.", innerException);
