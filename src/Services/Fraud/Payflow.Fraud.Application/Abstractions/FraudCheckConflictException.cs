namespace Payflow.Fraud.Application.Abstractions;

/// <summary>Thrown when a fraud check for a PaymentId loses a race against a concurrent redelivery of the same command.</summary>
public sealed class FraudCheckConflictException(Guid paymentId, Exception innerException)
    : Exception($"A fraud check for payment '{paymentId}' was already recorded by a concurrent request.", innerException);
