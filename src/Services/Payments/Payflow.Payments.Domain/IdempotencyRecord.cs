using Payflow.Shared.Kernel;

namespace Payflow.Payments.Domain;

/// <summary>
/// Records that a given (merchant, Idempotency-Key) pair has already produced a result, and what
/// that result was, so a retried request (client timeout, network blip, at-least-once redelivery)
/// replays the original response instead of double-charging. Enforced at the storage layer with a
/// unique constraint on (MerchantId, Key) – see Payflow.Payments.Infrastructure.
/// </summary>
public sealed class IdempotencyRecord : Entity<Guid>
{
    public string MerchantId { get; private set; } = null!;
    public string Key { get; private set; } = null!;
    public Guid PaymentId { get; private set; }
    public int ResponseStatusCode { get; private set; }
    public string ResponseBody { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord() { } // EF Core

    private IdempotencyRecord(Guid id, string merchantId, string key, Guid paymentId, int responseStatusCode, string responseBody)
        : base(id)
    {
        MerchantId = merchantId;
        Key = key;
        PaymentId = paymentId;
        ResponseStatusCode = responseStatusCode;
        ResponseBody = responseBody;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static IdempotencyRecord Create(string merchantId, string key, Guid paymentId, int responseStatusCode, string responseBody) =>
        new(Guid.NewGuid(), merchantId, key, paymentId, responseStatusCode, responseBody);
}
