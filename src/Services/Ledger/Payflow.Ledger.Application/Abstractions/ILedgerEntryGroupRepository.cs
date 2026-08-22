using Payflow.Ledger.Domain;

namespace Payflow.Ledger.Application.Abstractions;

public interface ILedgerEntryGroupRepository
{
    Task AddAsync(LedgerEntryGroup group, CancellationToken cancellationToken);
    Task<LedgerEntryGroup?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);
}
