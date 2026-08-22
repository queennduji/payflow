using MediatR;
using Payflow.Fraud.Application.Abstractions;
using Payflow.Shared.Kernel;

namespace Payflow.Fraud.Application.FraudChecks;

public sealed record GetFraudCheckByPaymentIdQuery(Guid PaymentId) : IRequest<Result<FraudCheckResult>>;

public sealed class GetFraudCheckByPaymentIdQueryHandler(IFraudCheckRepository repository)
    : IRequestHandler<GetFraudCheckByPaymentIdQuery, Result<FraudCheckResult>>
{
    public async Task<Result<FraudCheckResult>> Handle(GetFraudCheckByPaymentIdQuery request, CancellationToken cancellationToken)
    {
        var record = await repository.FindByPaymentIdAsync(request.PaymentId, cancellationToken);
        if (record is null)
            return Result.Failure<FraudCheckResult>(Error.NotFound("FraudCheck.NotFound", $"No fraud check recorded for payment '{request.PaymentId}'."));

        return Result.Success(new FraudCheckResult(record.Approved, record.Reason));
    }
}
