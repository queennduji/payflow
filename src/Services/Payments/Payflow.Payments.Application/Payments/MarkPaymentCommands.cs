using MediatR;
using Payflow.Payments.Application.Abstractions;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.Application.Payments;

// Thin commands the saga (Payflow.Payments.Application.Saga) uses to keep the Payment aggregate's
// own status in sync with saga progress, so GET /payments/{id} always reflects the current state
// regardless of whether the caller is still waiting on the synchronous response or polling after a
// 202 (see ADR-0006). Each handler follows the same fetch-mutate-save shape as the domain method it
// wraps and is a no-op-safe target for at-least-once redelivery: Payment's own guard clauses reject
// an out-of-order transition rather than corrupting state.

public sealed record MarkPaymentAuthorizedCommand(Guid PaymentId, Guid AuthorizationId) : IRequest<Result>;

public sealed class MarkPaymentAuthorizedCommandHandler(IPaymentRepository payments, IUnitOfWork unitOfWork)
    : IRequestHandler<MarkPaymentAuthorizedCommand, Result>
{
    public async Task<Result> Handle(MarkPaymentAuthorizedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        var result = payment.Authorize(request.AuthorizationId);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record MarkPaymentDeclinedCommand(Guid PaymentId, string Reason) : IRequest<Result>;

public sealed class MarkPaymentDeclinedCommandHandler(IPaymentRepository payments, IUnitOfWork unitOfWork)
    : IRequestHandler<MarkPaymentDeclinedCommand, Result>
{
    public async Task<Result> Handle(MarkPaymentDeclinedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        var result = payment.Decline(request.Reason);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record MarkPaymentCapturedCommand(Guid PaymentId) : IRequest<Result>;

public sealed class MarkPaymentCapturedCommandHandler(IPaymentRepository payments, IUnitOfWork unitOfWork)
    : IRequestHandler<MarkPaymentCapturedCommand, Result>
{
    public async Task<Result> Handle(MarkPaymentCapturedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        var result = payment.Capture();
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record MarkPaymentFailedCommand(Guid PaymentId, string Reason) : IRequest<Result>;

public sealed class MarkPaymentFailedCommandHandler(IPaymentRepository payments, IUnitOfWork unitOfWork)
    : IRequestHandler<MarkPaymentFailedCommand, Result>
{
    public async Task<Result> Handle(MarkPaymentFailedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        var result = payment.Fail(request.Reason);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
