namespace Payflow.Shared.Kernel;

/// <summary>
/// An amount in a specific ISO-4217 currency. Arithmetic across mismatched currencies throws
/// rather than silently producing a nonsensical amount — money bugs are the ones that get noticed.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount <= 0)
            return Result.Failure<Money>(Error.Validation("Money.NonPositive", "Amount must be greater than zero."));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Failure<Money>(Error.Validation("Money.InvalidCurrency", "Currency must be a 3-letter ISO-4217 code."));

        return Result.Success(new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), currency.ToUpperInvariant()));
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot combine amounts in {Currency} and {other.Currency}.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
