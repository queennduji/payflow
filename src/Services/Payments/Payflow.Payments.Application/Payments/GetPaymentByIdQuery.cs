using MediatR;
using Payflow.Payments.Application.Abstractions;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.Application.Payments;

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<Result<PaymentResponse>>;

public sealed class GetPaymentByIdQueryHandler(IPaymentRepository payments)
    : IRequestHandler<GetPaymentByIdQuery, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure<PaymentResponse>(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        return Result.Success(new PaymentResponse(
            payment.Id,
            payment.MerchantId,
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.Status.ToString(),
            payment.AuthorizationId,
            payment.FailureReason,
            payment.CreatedAt));
    }
}
