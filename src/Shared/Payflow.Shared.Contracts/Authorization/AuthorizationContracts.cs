namespace Payflow.Shared.Contracts.Authorization;

/// <summary>Request sent by Payments to the Authorization service to authorize a card charge.</summary>
public sealed record AuthorizeRequest(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string PaymentMethodRef);

/// <summary>Outcome of an authorization attempt against the (mock) card network.</summary>
public sealed record AuthorizeResponse(
    Guid AuthorizationId,
    bool Approved,
    string? DeclineReason,
    string ProcessorReference);
