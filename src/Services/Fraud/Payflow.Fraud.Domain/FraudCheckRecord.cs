using Payflow.Shared.Kernel;

namespace Payflow.Fraud.Domain;

/// <summary>
/// Audit record of a single fraud review, and the source of truth for two things at once: the
/// idempotent-consumer check (has this PaymentId already been evaluated?) and the velocity check
/// (how many recent attempts has this merchant made?).
/// </summary>
public sealed class FraudCheckRecord : Entity<Guid>
{
    public Guid PaymentId { get; private set; }
    public string MerchantId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public bool Approved { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private FraudCheckRecord() { } // EF Core

    private FraudCheckRecord(Guid id, Guid paymentId, string merchantId, decimal amount, string currency, bool approved, string? reason)
        : base(id)
    {
        PaymentId = paymentId;
        MerchantId = merchantId;
        Amount = amount;
        Currency = currency;
        Approved = approved;
        Reason = reason;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static FraudCheckRecord Record(Guid paymentId, string merchantId, decimal amount, string currency, bool approved, string? reason) =>
        new(Guid.NewGuid(), paymentId, merchantId, amount, currency, approved, reason);
}
