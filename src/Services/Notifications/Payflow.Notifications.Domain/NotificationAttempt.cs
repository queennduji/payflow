using Payflow.Shared.Kernel;

namespace Payflow.Notifications.Domain;

/// <summary>
/// A simulated merchant webhook dispatch – there's no real external endpoint to call in this demo,
/// so "sending" means durably recording that the notification would have gone out. Phase 3 adds
/// Polly retry semantics on top of this for the (still simulated) delivery step.
/// </summary>
public sealed class NotificationAttempt : Entity<Guid>
{
    public Guid PaymentId { get; private set; }
    public string MerchantId { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public DateTimeOffset SentAt { get; private set; }

    private NotificationAttempt() { } // EF Core

    private NotificationAttempt(Guid id, Guid paymentId, string merchantId, string status) : base(id)
    {
        PaymentId = paymentId;
        MerchantId = merchantId;
        Status = status;
        SentAt = DateTimeOffset.UtcNow;
    }

    public static NotificationAttempt Record(Guid paymentId, string merchantId, string status) =>
        new(Guid.NewGuid(), paymentId, merchantId, status);
}
