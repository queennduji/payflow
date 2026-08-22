namespace Payflow.Notifications.Application.Abstractions;

/// <summary>Thrown when recording a notification for a PaymentId loses a race against a concurrent redelivery.</summary>
public sealed class NotificationConflictException(Guid paymentId, Exception innerException)
    : Exception($"A notification for payment '{paymentId}' was already recorded by a concurrent request.", innerException);
