using Payflow.Shared.Kernel;

namespace Payflow.Payments.Domain;

/// <summary>
/// The Payment aggregate. Owns the lifecycle of a single payment attempt from submission through
/// authorization, capture, or failure. It does not know how authorization or ledger posting are
/// actually performed — those are separate bounded contexts invoked by the Application layer; this
/// aggregate only enforces which state transitions are legal.
/// </summary>
public sealed class Payment : AggregateRoot<Guid>
{
    public string MerchantId { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public string PaymentMethodRef { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public Guid? AuthorizationId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Payment() { } // EF Core

    private Payment(Guid id, string merchantId, Money amount, string paymentMethodRef, string idempotencyKey)
        : base(id)
    {
        MerchantId = merchantId;
        Amount = amount;
        PaymentMethodRef = paymentMethodRef;
        IdempotencyKey = idempotencyKey;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Payment> Submit(string merchantId, Money amount, string paymentMethodRef, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            return Result.Failure<Payment>(Error.Validation("Payment.MerchantIdRequired", "MerchantId is required."));

        if (string.IsNullOrWhiteSpace(paymentMethodRef))
            return Result.Failure<Payment>(Error.Validation("Payment.PaymentMethodRequired", "PaymentMethodRef is required."));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result.Failure<Payment>(Error.Validation("Payment.IdempotencyKeyRequired", "Idempotency-Key header is required."));

        var payment = new Payment(Guid.NewGuid(), merchantId, amount, paymentMethodRef, idempotencyKey);
        payment.Raise(PaymentSubmitted.For(payment));
        return Result.Success(payment);
    }

    public Result Authorize(Guid authorizationId)
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(Error.Conflict("Payment.InvalidTransition", $"Cannot authorize a payment in status {Status}."));

        AuthorizationId = authorizationId;
        Status = PaymentStatus.Authorized;
        Touch();
        Raise(PaymentAuthorized.For(this, authorizationId));
        return Result.Success();
    }

    public Result Decline(string reason)
    {
        if (Status != PaymentStatus.Pending)
            return Result.Failure(Error.Conflict("Payment.InvalidTransition", $"Cannot decline a payment in status {Status}."));

        FailureReason = reason;
        Status = PaymentStatus.Declined;
        Touch();
        Raise(PaymentDeclined.For(this, reason));
        return Result.Success();
    }

    public Result Capture()
    {
        if (Status != PaymentStatus.Authorized)
            return Result.Failure(Error.Conflict("Payment.InvalidTransition", $"Cannot capture a payment in status {Status}."));

        Status = PaymentStatus.Captured;
        Touch();
        Raise(PaymentCaptured.For(this));
        return Result.Success();
    }

    public Result Fail(string reason)
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Declined or PaymentStatus.Failed)
            return Result.Failure(Error.Conflict("Payment.InvalidTransition", $"Cannot fail a payment already in status {Status}."));

        FailureReason = reason;
        Status = PaymentStatus.Failed;
        Touch();
        Raise(PaymentFailed.For(this, reason));
        return Result.Success();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
