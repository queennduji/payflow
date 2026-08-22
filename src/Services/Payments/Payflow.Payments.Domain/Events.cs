using Payflow.Shared.Kernel;

namespace Payflow.Payments.Domain;

public sealed record PaymentSubmitted(Guid EventId, DateTimeOffset OccurredOn, Guid PaymentId, string MerchantId, decimal Amount, string Currency)
    : IDomainEvent
{
    public static PaymentSubmitted For(Payment payment) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, payment.Id, payment.MerchantId, payment.Amount.Amount, payment.Amount.Currency);
}

public sealed record PaymentAuthorized(Guid EventId, DateTimeOffset OccurredOn, Guid PaymentId, Guid AuthorizationId)
    : IDomainEvent
{
    public static PaymentAuthorized For(Payment payment, Guid authorizationId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, payment.Id, authorizationId);
}

public sealed record PaymentDeclined(Guid EventId, DateTimeOffset OccurredOn, Guid PaymentId, string Reason)
    : IDomainEvent
{
    public static PaymentDeclined For(Payment payment, string reason) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, payment.Id, reason);
}

public sealed record PaymentCaptured(Guid EventId, DateTimeOffset OccurredOn, Guid PaymentId)
    : IDomainEvent
{
    public static PaymentCaptured For(Payment payment) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, payment.Id);
}

public sealed record PaymentFailed(Guid EventId, DateTimeOffset OccurredOn, Guid PaymentId, string Reason)
    : IDomainEvent
{
    public static PaymentFailed For(Payment payment, string reason) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, payment.Id, reason);
}
