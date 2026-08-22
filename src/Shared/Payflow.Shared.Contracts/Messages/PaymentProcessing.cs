namespace Payflow.Shared.Contracts.Messages;

/// <summary>
/// Sent by Payments.Api via <c>IRequestClient&lt;ProcessPayment&gt;</c> to both start the payment
/// saga and, eventually, receive its outcome. This is the one message the saga's state machine
/// binds to with <c>Initially()</c> — everything else in the flow is point-to-point commands and
/// their result events between the saga and the participating services.
/// </summary>
public sealed record ProcessPayment(
    Guid PaymentId,
    string MerchantId,
    decimal Amount,
    string Currency,
    string PaymentMethodRef);

/// <summary>
/// The saga's response to the original <see cref="ProcessPayment"/> request, sent from whichever
/// step happens to finalize the saga (not necessarily the step that received the request) — the
/// saga carries the requester's <c>ResponseAddress</c>/<c>RequestId</c> forward in its own state
/// for exactly this purpose. See ADR-0006.
/// </summary>
public sealed record PaymentProcessed(Guid PaymentId, string Status, string? FailureReason);
