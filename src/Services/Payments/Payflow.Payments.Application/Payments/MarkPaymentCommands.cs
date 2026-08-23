using MediatR;
using Payflow.Payments.Application.Abstractions;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.Application.Payments;

// Thin commands the saga (Payflow.Payments.Application.Saga) uses to keep the Payment aggregate's
// own status in sync with saga progress, so GET /payments/{id} always reflects the current state
// regardless of whether the caller is still waiting on the synchronous response or polling after a
// 202 (see ADR-0006). Each handler follows the same fetch-mutate shape as the domain method it
// wraps and is a no-op-safe target for at-least-once redelivery: Payment's own guard clauses reject
// an out-of-order transition rather than corrupting state.
//
// Deliberately no IUnitOfWork.SaveChangesAsync() here: these commands only ever run from inside the
// saga's own activity chain, sharing the same DbContext instance MassTransit's EF saga repository
// is already going to save and commit once the whole consumer pipeline finishes. Calling
// SaveChangesAsync ourselves mid-pipeline nests a commit inside that still-open ambient
// transaction — which doesn't actually commit anything early, but does trip the transactional
// outbox's immediate-dispatch optimization into delivering messages before the real outer commit
// happens, defeating the very atomicity the outbox exists to provide. Leaving the mutation tracked
// and letting the saga repository's own SaveChanges pick it up keeps everything in the one true
// commit. See ADR-0005/ADR-0006.

public sealed record MarkPaymentAuthorizedCommand(Guid PaymentId, Guid AuthorizationId) : IRequest<Result>;

public sealed class MarkPaymentAuthorizedCommandHandler(IPaymentRepository payments)
    : IRequestHandler<MarkPaymentAuthorizedCommand, Result>
{
    public async Task<Result> Handle(MarkPaymentAuthorizedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        return payment.Authorize(request.AuthorizationId);
    }
}

public sealed record MarkPaymentDeclinedCommand(Guid PaymentId, string Reason) : IRequest<Result>;

public sealed class MarkPaymentDeclinedCommandHandler(IPaymentRepository payments)
    : IRequestHandler<MarkPaymentDeclinedCommand, Result>
{
    public async Task<Result> Handle(MarkPaymentDeclinedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        return payment.Decline(request.Reason);
    }
}

public sealed record MarkPaymentCapturedCommand(Guid PaymentId) : IRequest<Result>;

public sealed class MarkPaymentCapturedCommandHandler(IPaymentRepository payments)
    : IRequestHandler<MarkPaymentCapturedCommand, Result>
{
    public async Task<Result> Handle(MarkPaymentCapturedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        return payment.Capture();
    }
}

public sealed record MarkPaymentFailedCommand(Guid PaymentId, string Reason) : IRequest<Result>;

public sealed class MarkPaymentFailedCommandHandler(IPaymentRepository payments)
    : IRequestHandler<MarkPaymentFailedCommand, Result>
{
    public async Task<Result> Handle(MarkPaymentFailedCommand request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("Payment.NotFound", $"No payment with id {request.PaymentId}."));

        return payment.Fail(request.Reason);
    }
}
