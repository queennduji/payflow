using Payflow.Shared.Kernel;

namespace Payflow.Ledger.Domain;

/// <summary>
/// A named position in the ledger. Deliberately holds no balance field — balance is always derived
/// by summing posted <see cref="LedgerLine"/>s (see <see cref="AccountBalanceCalculator"/>), so it
/// can never drift out of sync with the entries that are supposed to explain it.
/// </summary>
/// <remarks>
/// Account provisioning is simplified for this demo: accounts are auto-opened on first reference
/// (see Payflow.Ledger.Infrastructure) with a type inferred from an id prefix convention. A
/// production ledger would require accounts to be explicitly opened by a chart-of-accounts process.
/// </remarks>
public sealed class Account : AggregateRoot<string>
{
    public string DisplayName { get; private set; } = null!;
    public AccountType Type { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Account() { } // EF Core

    private Account(string id, string displayName, AccountType type, string currency) : base(id)
    {
        DisplayName = displayName;
        Type = type;
        Currency = currency;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Account Open(string id, string displayName, AccountType type, string currency) =>
        new(id, displayName, type, currency);
}
