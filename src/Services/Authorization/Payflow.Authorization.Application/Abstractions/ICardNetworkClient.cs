namespace Payflow.Authorization.Application.Abstractions;

/// <summary>
/// The (mocked) external dependency: a real card network call, with the latency and occasional
/// unavailability a real one would have. <see cref="AuthorizePaymentCommandHandler"/> depends only
/// on this interface – it has no idea whether the implementation behind it is resilient, flaky, or
/// both at once.
/// </summary>
public interface ICardNetworkClient
{
    Task<CardNetworkResult> AuthorizeAsync(decimal amount, string currency, string paymentMethodRef, CancellationToken cancellationToken);
}

public sealed record CardNetworkResult(bool Approved, string? DeclineReason);

/// <summary>Thrown by the simulated network client to stand in for a timeout, connection reset, or 5xx from a real processor.</summary>
public sealed class CardNetworkUnavailableException(string message) : Exception(message);
