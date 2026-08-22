namespace Payflow.Shared.Contracts.Messages;

public sealed record PostLedgerEntry(Guid PaymentId, string DebitAccountId, string CreditAccountId, decimal Amount, string Currency);

public sealed record LedgerEntryPosted(Guid PaymentId, Guid EntryGroupId);

public sealed record LedgerPostFailed(Guid PaymentId, string Reason);
