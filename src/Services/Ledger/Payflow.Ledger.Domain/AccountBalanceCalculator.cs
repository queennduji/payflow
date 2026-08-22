namespace Payflow.Ledger.Domain;

/// <summary>
/// Pure accounting math, kept free of persistence so the debit/credit-sign rules can be unit
/// tested without a database: Asset and Expense accounts are debit-normal (a debit increases the
/// balance); Liability, Equity, and Revenue accounts are credit-normal (a credit increases it).
/// </summary>
public static class AccountBalanceCalculator
{
    public static decimal Compute(AccountType accountType, IEnumerable<(LedgerDirection Direction, decimal Amount)> lines)
    {
        decimal debitTotal = 0m;
        decimal creditTotal = 0m;

        foreach (var (direction, amount) in lines)
        {
            if (direction == LedgerDirection.Debit) debitTotal += amount;
            else creditTotal += amount;
        }

        var isDebitNormal = accountType is AccountType.Asset or AccountType.Expense;
        return isDebitNormal ? debitTotal - creditTotal : creditTotal - debitTotal;
    }
}
