namespace Payflow.Payments.Application.Payments;

public sealed record PaymentResponse(
    Guid PaymentId,
    string MerchantId,
    decimal Amount,
    string Currency,
    string Status,
    Guid? AuthorizationId,
    string? FailureReason,
    DateTimeOffset CreatedAt);
