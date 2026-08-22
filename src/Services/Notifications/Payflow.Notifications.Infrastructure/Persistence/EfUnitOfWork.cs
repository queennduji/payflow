using Microsoft.EntityFrameworkCore;
using Npgsql;
using Payflow.Notifications.Application.Abstractions;
using Payflow.Notifications.Domain;

namespace Payflow.Notifications.Infrastructure.Persistence;

public sealed class EfUnitOfWork(NotificationsDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            var conflicting = db.ChangeTracker.Entries<NotificationAttempt>()
                .FirstOrDefault(e => e.State == EntityState.Added)?.Entity;

            if (conflicting is null)
                throw;

            throw new NotificationConflictException(conflicting.PaymentId, ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
