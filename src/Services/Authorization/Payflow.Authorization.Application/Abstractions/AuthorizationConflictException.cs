namespace Payflow.Authorization.Application.Abstractions;

/// <summary>Thrown when recording an authorization decision for a PaymentId loses a race against a concurrent redelivery.</summary>
public sealed class AuthorizationConflictException(Guid paymentId, Exception innerException)
    : Exception($"An authorization decision for payment '{paymentId}' was already recorded by a concurrent request.", innerException);
