using Payflow.Shared.Kernel;

namespace Payflow.Vault.Domain;

/// <summary>
/// A tokenized card reference. This is the entire cardholder-data footprint the platform is
/// willing to carry: the full card number handed to <see cref="Issue"/> is used only to compute
/// <see cref="Last4"/> and is never assigned to a field, logged, or returned — there is no field
/// on this type it could even be stored in by accident.
/// </summary>
public sealed class VaultToken : Entity<Guid>
{
    public string Token { get; private set; } = null!;
    public string Last4 { get; private set; } = null!;
    public int ExpiryMonth { get; private set; }
    public int ExpiryYear { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private VaultToken() { } // EF Core

    private VaultToken(Guid id, string token, string last4, int expiryMonth, int expiryYear) : base(id)
    {
        Token = token;
        Last4 = last4;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static VaultToken Issue(string cardNumber, int expiryMonth, int expiryYear)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4 || !cardNumber.All(char.IsDigit))
            throw new ArgumentException("Card number must be a digit string of at least 4 digits.", nameof(cardNumber));

        if (expiryMonth is < 1 or > 12)
            throw new ArgumentException("Expiry month must be between 1 and 12.", nameof(expiryMonth));

        var last4 = cardNumber[^4..];
        var token = $"tok_{Guid.NewGuid():N}";
        return new VaultToken(Guid.NewGuid(), token, last4, expiryMonth, expiryYear);
    }
}
