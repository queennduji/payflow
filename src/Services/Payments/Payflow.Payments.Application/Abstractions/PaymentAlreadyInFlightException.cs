namespace Payflow.Payments.Application.Abstractions;

/// <summary>
/// Thrown when creating a new <see cref="Payflow.Payments.Domain.Payment"/> loses a race against a
/// concurrent request for the same (merchant, Idempotency-Key) pair. This is what closes the
/// Phase-1 retry gap (ADR-0002): rather than starting a second authorization, the caller re-fetches
/// the winning Payment and either replays its result (if terminal) or reports it as still in
/// flight (see ADR-0006's 202 fallback).
/// </summary>
public sealed class PaymentAlreadyInFlightException(string merchantId, string idempotencyKey, Exception innerException)
    : Exception($"A payment for merchant '{merchantId}' with idempotency key '{idempotencyKey}' is already in flight.", innerException);
