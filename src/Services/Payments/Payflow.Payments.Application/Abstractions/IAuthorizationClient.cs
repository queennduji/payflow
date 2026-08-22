using Payflow.Shared.Contracts.Authorization;

namespace Payflow.Payments.Application.Abstractions;

/// <summary>
/// Outbound port to the Authorization service. Phase 1 implementation is a synchronous HTTP call;
/// Phase 2 replaces the call site with a saga step over the message broker without touching this
/// interface's callers.
/// </summary>
public interface IAuthorizationClient
{
    Task<AuthorizeResponse> AuthorizeAsync(AuthorizeRequest request, CancellationToken cancellationToken);
}
