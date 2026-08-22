using FluentAssertions;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.UnitTests.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_rejects_non_positive_amounts(decimal amount)
    {
        var result = Money.Create(amount, "USD");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.NonPositive");
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLARS")]
    public void Create_rejects_invalid_currency_codes(string currency)
    {
        var result = Money.Create(10, currency);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.InvalidCurrency");
    }

    [Fact]
    public void Create_rounds_to_two_decimal_places_and_uppercases_currency()
    {
        var result = Money.Create(19.999m, "usd");

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(20.00m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_throws_when_currencies_differ()
    {
        var usd = Money.Create(10, "USD").Value;
        var eur = Money.Create(10, "EUR").Value;

        var act = () => usd.Add(eur);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Two_amounts_with_same_value_and_currency_are_equal()
    {
        var a = Money.Create(42.5m, "USD").Value;
        var b = Money.Create(42.5m, "usd").Value;

        a.Should().Be(b);
    }
}
