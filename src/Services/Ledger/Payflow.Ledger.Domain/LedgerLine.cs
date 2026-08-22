using Payflow.Shared.Kernel;

namespace Payflow.Ledger.Domain;

public enum LedgerDirection
{
    Debit,
    Credit
}

/// <summary>One side of a balanced posting. Always belongs to exactly one <see cref="LedgerEntryGroup"/>.</summary>
public sealed class LedgerLine
{
    public string AccountId { get; private set; } = null!;
    public LedgerDirection Direction { get; private set; }
    public Money Amount { get; private set; } = null!;

    private LedgerLine() { } // EF Core

    private LedgerLine(string accountId, LedgerDirection direction, Money amount)
    {
        AccountId = accountId;
        Direction = direction;
        Amount = amount;
    }

    public static LedgerLine Debit(string accountId, Money amount) => new(accountId, LedgerDirection.Debit, amount);
    public static LedgerLine Credit(string accountId, Money amount) => new(accountId, LedgerDirection.Credit, amount);
}
