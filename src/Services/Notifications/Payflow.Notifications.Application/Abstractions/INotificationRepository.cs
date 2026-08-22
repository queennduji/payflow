using Payflow.Notifications.Domain;

namespace Payflow.Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task<NotificationAttempt?> FindByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationAttempt>> ListByMerchantAsync(string merchantId, CancellationToken cancellationToken);
    Task AddAsync(NotificationAttempt attempt, CancellationToken cancellationToken);
}
