using Microsoft.EntityFrameworkCore;
using Npgsql;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Domain;

namespace Payflow.Payments.Infrastructure.Persistence;

public sealed class EfUnitOfWork(PaymentsDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Translate the storage-level race into the Application-layer vocabulary before it
            // escapes this layer — callers should never need to know we're on Postgres. Each call
            // site only ever adds one of these in a given SaveChangesAsync, so checking both is
            // unambiguous.
            var conflictingIdempotency = db.ChangeTracker.Entries<IdempotencyRecord>()
                .FirstOrDefault(e => e.State == EntityState.Added)?.Entity;
            if (conflictingIdempotency is not null)
                throw new IdempotencyKeyConflictException(conflictingIdempotency.MerchantId, conflictingIdempotency.Key, ex);

            var conflictingPayment = db.ChangeTracker.Entries<Payment>()
                .FirstOrDefault(e => e.State == EntityState.Added)?.Entity;
            if (conflictingPayment is not null)
                throw new PaymentAlreadyInFlightException(conflictingPayment.MerchantId, conflictingPayment.IdempotencyKey, ex);

            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
