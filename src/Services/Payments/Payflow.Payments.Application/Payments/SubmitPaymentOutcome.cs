namespace Payflow.Payments.Application.Payments;

/// <summary>
/// What <see cref="SubmitPaymentCommandHandler"/> can report back: either the saga finished within
/// the bounded wait and there's a definitive result, or it didn't (or a concurrent in-flight
/// attempt was found) and the caller should poll instead. See ADR-0006.
/// </summary>
public abstract record SubmitPaymentOutcome;

public sealed record SubmitPaymentCompleted(PaymentResponse Payment) : SubmitPaymentOutcome;

public sealed record SubmitPaymentPending(Guid PaymentId) : SubmitPaymentOutcome;
