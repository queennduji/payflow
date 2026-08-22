using MediatR;
using Payflow.Shared.Kernel;

namespace Payflow.Fraud.Application.FraudChecks;

public sealed record EvaluateFraudCommand(
    Guid PaymentId,
    string MerchantId,
    decimal Amount,
    string Currency,
    string PaymentMethodRef) : IRequest<Result<FraudCheckResult>>;

public sealed record FraudCheckResult(bool Approved, string? Reason);
