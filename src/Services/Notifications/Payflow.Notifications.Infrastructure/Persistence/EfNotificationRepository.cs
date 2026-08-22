using Microsoft.EntityFrameworkCore;
using Payflow.Notifications.Application.Abstractions;
using Payflow.Notifications.Domain;

namespace Payflow.Notifications.Infrastructure.Persistence;

public sealed class EfNotificationRepository(NotificationsDbContext db) : INotificationRepository
{
    public Task<NotificationAttempt?> FindByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        db.NotificationAttempts.SingleOrDefaultAsync(a => a.PaymentId == paymentId, cancellationToken);

    public async Task<IReadOnlyList<NotificationAttempt>> ListByMerchantAsync(string merchantId, CancellationToken cancellationToken) =>
        await db.NotificationAttempts
            .Where(a => a.MerchantId == merchantId)
            .OrderByDescending(a => a.SentAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(NotificationAttempt attempt, CancellationToken cancellationToken) =>
        await db.NotificationAttempts.AddAsync(attempt, cancellationToken);
}
