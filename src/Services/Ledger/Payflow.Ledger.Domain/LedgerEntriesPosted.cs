using Payflow.Shared.Kernel;

namespace Payflow.Ledger.Domain;

public sealed record LedgerEntriesPosted(Guid EventId, DateTimeOffset OccurredOn, Guid LedgerEntryGroupId, Guid PaymentId)
    : IDomainEvent
{
    public static LedgerEntriesPosted For(LedgerEntryGroup group) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, group.Id, group.PaymentId);
}
