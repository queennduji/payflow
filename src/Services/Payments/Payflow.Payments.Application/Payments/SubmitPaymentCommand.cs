using MediatR;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.Application.Payments;

public sealed record SubmitPaymentCommand(
    string MerchantId,
    decimal Amount,
    string Currency,
    string PaymentMethodRef,
    string IdempotencyKey) : IRequest<Result<SubmitPaymentOutcome>>;
