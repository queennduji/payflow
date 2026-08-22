using FluentAssertions;
using Payflow.Ledger.Domain;
using Payflow.Shared.Kernel;

namespace Payflow.Ledger.UnitTests;

public class LedgerEntryGroupTests
{
    private static Money Usd(decimal amount) => Money.Create(amount, "USD").Value;

    [Fact]
    public void Post_succeeds_when_debits_equal_credits()
    {
        var lines = new[]
        {
            LedgerLine.Debit("customer:tok_1", Usd(100)),
            LedgerLine.Credit("merchant:acme", Usd(100))
        };

        var result = LedgerEntryGroup.Post(Guid.NewGuid(), lines);

        result.IsSuccess.Should().BeTrue();
        result.Value.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void Post_rejects_unbalanced_debits_and_credits()
    {
        var lines = new[]
        {
            LedgerLine.Debit("customer:tok_1", Usd(100)),
            LedgerLine.Credit("merchant:acme", Usd(90))
        };

        var result = LedgerEntryGroup.Post(Guid.NewGuid(), lines);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ledger.Unbalanced");
    }

    [Fact]
    public void Post_rejects_a_single_line()
    {
        var lines = new[] { LedgerLine.Debit("customer:tok_1", Usd(100)) };

        var result = LedgerEntryGroup.Post(Guid.NewGuid(), lines);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ledger.TooFewLines");
    }

    [Fact]
    public void Post_rejects_mixed_currencies()
    {
        var lines = new[]
        {
            LedgerLine.Debit("customer:tok_1", Usd(100)),
            LedgerLine.Credit("merchant:acme", Money.Create(100, "EUR").Value)
        };

        var result = LedgerEntryGroup.Post(Guid.NewGuid(), lines);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ledger.CurrencyMismatch");
    }

    [Fact]
    public void Post_raises_LedgerEntriesPosted()
    {
        var lines = new[]
        {
            LedgerLine.Debit("customer:tok_1", Usd(50)),
            LedgerLine.Credit("merchant:acme", Usd(50))
        };

        var group = LedgerEntryGroup.Post(Guid.NewGuid(), lines).Value;

        group.DomainEvents.Should().ContainSingle(e => e is LedgerEntriesPosted);
    }
}
