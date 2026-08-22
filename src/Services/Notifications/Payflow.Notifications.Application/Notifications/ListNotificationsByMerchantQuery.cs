using MediatR;
using Payflow.Notifications.Application.Abstractions;
using Payflow.Shared.Kernel;

namespace Payflow.Notifications.Application.Notifications;

public sealed record ListNotificationsByMerchantQuery(string MerchantId) : IRequest<Result<IReadOnlyList<NotificationResult>>>;

public sealed class ListNotificationsByMerchantQueryHandler(INotificationRepository repository)
    : IRequestHandler<ListNotificationsByMerchantQuery, Result<IReadOnlyList<NotificationResult>>>
{
    public async Task<Result<IReadOnlyList<NotificationResult>>> Handle(ListNotificationsByMerchantQuery request, CancellationToken cancellationToken)
    {
        var attempts = await repository.ListByMerchantAsync(request.MerchantId, cancellationToken);
        IReadOnlyList<NotificationResult> results = attempts
            .Select(a => new NotificationResult(a.PaymentId, a.MerchantId, a.Status, a.SentAt))
            .ToList();

        return Result.Success(results);
    }
}
