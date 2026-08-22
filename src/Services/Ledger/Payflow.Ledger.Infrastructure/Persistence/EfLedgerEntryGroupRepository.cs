using Microsoft.EntityFrameworkCore;
using Payflow.Ledger.Application.Abstractions;
using Payflow.Ledger.Domain;

namespace Payflow.Ledger.Infrastructure.Persistence;

public sealed class EfLedgerEntryGroupRepository(LedgerDbContext db) : ILedgerEntryGroupRepository
{
    public async Task AddAsync(LedgerEntryGroup group, CancellationToken cancellationToken) =>
        await db.LedgerEntryGroups.AddAsync(group, cancellationToken);

    // Owned collections (Lines) are loaded automatically — no Include needed.
    public Task<LedgerEntryGroup?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        db.LedgerEntryGroups.SingleOrDefaultAsync(g => g.PaymentId == paymentId, cancellationToken);
}
