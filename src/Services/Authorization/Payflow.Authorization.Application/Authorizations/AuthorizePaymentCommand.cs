using MediatR;
using Payflow.Shared.Kernel;

namespace Payflow.Authorization.Application.Authorizations;

public sealed record AuthorizePaymentCommand(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string PaymentMethodRef) : IRequest<Result<AuthorizationResult>>;

public sealed record AuthorizationResult(Guid AuthorizationId, bool Approved, string? DeclineReason, string ProcessorReference);
