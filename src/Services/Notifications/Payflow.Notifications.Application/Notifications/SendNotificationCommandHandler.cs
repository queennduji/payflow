using MediatR;
using Payflow.Notifications.Application.Abstractions;
using Payflow.Notifications.Domain;
using Payflow.Shared.Kernel;

namespace Payflow.Notifications.Application.Notifications;

/// <summary>Idempotent per PaymentId – the saga's fire-and-forget publish could still be redelivered at least once.</summary>
public sealed class SendNotificationCommandHandler(INotificationRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<SendNotificationCommand, Result<NotificationResult>>
{
    public async Task<Result<NotificationResult>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByPaymentIdAsync(request.PaymentId, cancellationToken);
        if (existing is not null)
            return Result.Success(ToResult(existing));

        var attempt = NotificationAttempt.Record(request.PaymentId, request.MerchantId, request.Status);
        await repository.AddAsync(attempt, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (NotificationConflictException)
        {
            var winner = await repository.FindByPaymentIdAsync(request.PaymentId, cancellationToken)
                ?? throw new InvalidOperationException("Lost a notification race but the winning record is missing.");
            return Result.Success(ToResult(winner));
        }

        return Result.Success(ToResult(attempt));
    }

    private static NotificationResult ToResult(NotificationAttempt attempt) =>
        new(attempt.PaymentId, attempt.MerchantId, attempt.Status, attempt.SentAt);
}
