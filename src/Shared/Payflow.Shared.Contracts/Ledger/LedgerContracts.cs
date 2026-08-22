namespace Payflow.Shared.Contracts.Ledger;

/// <summary>
/// Request to post a balanced (debit = credit) pair of ledger entries for a settled payment.
/// The Ledger service never accepts a single-sided entry — see ADR-0003.
/// </summary>
public sealed record PostLedgerEntryRequest(
    Guid PaymentId,
    string DebitAccountId,
    string CreditAccountId,
    decimal Amount,
    string Currency);

public sealed record PostLedgerEntryResponse(
    Guid EntryGroupId,
    bool Posted);

public sealed record AccountBalanceResponse(
    string AccountId,
    decimal Balance,
    string Currency);
