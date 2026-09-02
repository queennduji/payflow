using Payflow.Shared.Kernel;

namespace Payflow.Ledger.Domain;

/// <summary>
/// An immutable, balanced posting: one atomic unit of the ledger. Once <see cref="Post"/> succeeds
/// there is no way to edit or remove a line – correcting a mistake means posting a new, opposite
/// group, exactly as a real accounting ledger requires. Never posted twice for the same
/// <see cref="PaymentId"/> – enforced by a unique index in Infrastructure, making this the
/// idempotent receiver for Payments' (at-least-once) capture-and-post call.
/// </summary>
public sealed class LedgerEntryGroup : AggregateRoot<Guid>
{
    private readonly List<LedgerLine> _lines = [];

    public Guid PaymentId { get; private set; }
    public string Currency { get; private set; } = null!;
    public IReadOnlyList<LedgerLine> Lines => _lines.AsReadOnly();
    public DateTimeOffset CreatedAt { get; private set; }

    private LedgerEntryGroup() { } // EF Core

    private LedgerEntryGroup(Guid id, Guid paymentId, string currency) : base(id)
    {
        PaymentId = paymentId;
        Currency = currency;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<LedgerEntryGroup> Post(Guid paymentId, IReadOnlyList<LedgerLine> lines)
    {
        if (lines.Count < 2)
            return Result.Failure<LedgerEntryGroup>(
                Error.Validation("Ledger.TooFewLines", "A ledger entry group needs at least one debit and one credit line."));

        var currency = lines[0].Amount.Currency;
        if (lines.Any(l => l.Amount.Currency != currency))
            return Result.Failure<LedgerEntryGroup>(
                Error.Validation("Ledger.CurrencyMismatch", "All lines in a ledger entry group must share the same currency."));

        var totalDebits = lines.Where(l => l.Direction == LedgerDirection.Debit).Sum(l => l.Amount.Amount);
        var totalCredits = lines.Where(l => l.Direction == LedgerDirection.Credit).Sum(l => l.Amount.Amount);

        if (totalDebits != totalCredits)
            return Result.Failure<LedgerEntryGroup>(
                Error.Validation("Ledger.Unbalanced", $"Debits ({totalDebits}) must equal credits ({totalCredits})."));

        var group = new LedgerEntryGroup(Guid.NewGuid(), paymentId, currency);
        group._lines.AddRange(lines);
        group.Raise(LedgerEntriesPosted.For(group));
        return Result.Success(group);
    }
}
