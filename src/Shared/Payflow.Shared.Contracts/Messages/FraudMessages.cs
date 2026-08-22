namespace Payflow.Shared.Contracts.Messages;

public sealed record CheckFraud(Guid PaymentId, string MerchantId, decimal Amount, string Currency, string PaymentMethodRef);

public sealed record FraudCheckPassed(Guid PaymentId);

public sealed record FraudCheckFailed(Guid PaymentId, string Reason);
