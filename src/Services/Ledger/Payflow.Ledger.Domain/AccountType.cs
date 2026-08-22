namespace Payflow.Ledger.Domain;

/// <summary>
/// The five classical account types, which determine which side (debit or credit) increases the
/// account's balance. Asset/Expense accounts are debit-normal; Liability/Equity/Revenue accounts
/// are credit-normal. See <see cref="AccountBalanceCalculator"/>.
/// </summary>
public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}
