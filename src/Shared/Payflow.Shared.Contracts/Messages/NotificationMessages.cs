namespace Payflow.Shared.Contracts.Messages;

/// <summary>
/// Fire-and-forget: the saga publishes this once a payment reaches a terminal state and does not
/// wait on a reply. A merchant-facing webhook failing (or being slow) should never roll back money
/// that already moved.
/// </summary>
public sealed record SendPaymentNotification(Guid PaymentId, string MerchantId, string Status);

public sealed record PaymentNotificationSent(Guid PaymentId, string MerchantId);
