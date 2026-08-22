namespace Payflow.Shared.Contracts.Messages;

public sealed record AuthorizePayment(Guid PaymentId, decimal Amount, string Currency, string PaymentMethodRef);

public sealed record PaymentAuthorized(Guid PaymentId, Guid AuthorizationId, string ProcessorReference);

public sealed record PaymentAuthorizationDeclined(Guid PaymentId, string Reason);

/// <summary>The saga's compensating action for a payment that was authorized but couldn't be captured.</summary>
public sealed record VoidAuthorization(Guid PaymentId, Guid AuthorizationId);

public sealed record AuthorizationVoided(Guid PaymentId, Guid AuthorizationId);
