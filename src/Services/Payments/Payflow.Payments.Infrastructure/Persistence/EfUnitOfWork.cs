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
            // escapes this layer — callers should never need to know we're on Postgres.
            var conflicting = db.ChangeTracker.Entries<IdempotencyRecord>()
                .FirstOrDefault(e => e.State == EntityState.Added)?.Entity;

            if (conflicting is null)
                throw;

            throw new IdempotencyKeyConflictException(conflicting.MerchantId, conflicting.Key, ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
