using FluentAssertions;
using Payflow.Ledger.Domain;

namespace Payflow.Ledger.UnitTests;

public class AccountBalanceCalculatorTests
{
    [Theory]
    [InlineData(AccountType.Asset)]
    [InlineData(AccountType.Expense)]
    public void Debit_increases_balance_for_debit_normal_accounts(AccountType type)
    {
        var lines = new[] { (LedgerDirection.Debit, 100m), (LedgerDirection.Credit, 30m) };

        var balance = AccountBalanceCalculator.Compute(type, lines);

        balance.Should().Be(70m);
    }

    [Theory]
    [InlineData(AccountType.Liability)]
    [InlineData(AccountType.Equity)]
    [InlineData(AccountType.Revenue)]
    public void Credit_increases_balance_for_credit_normal_accounts(AccountType type)
    {
        var lines = new[] { (LedgerDirection.Debit, 30m), (LedgerDirection.Credit, 100m) };

        var balance = AccountBalanceCalculator.Compute(type, lines);

        balance.Should().Be(70m);
    }

    [Fact]
    public void No_lines_yields_a_zero_balance()
    {
        var balance = AccountBalanceCalculator.Compute(AccountType.Asset, []);

        balance.Should().Be(0m);
    }
}
