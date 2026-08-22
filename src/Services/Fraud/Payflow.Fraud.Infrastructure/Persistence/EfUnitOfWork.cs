using Microsoft.EntityFrameworkCore;
using Npgsql;
using Payflow.Fraud.Application.Abstractions;
using Payflow.Fraud.Domain;

namespace Payflow.Fraud.Infrastructure.Persistence;

public sealed class EfUnitOfWork(FraudDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            var conflicting = db.ChangeTracker.Entries<FraudCheckRecord>()
                .FirstOrDefault(e => e.State == EntityState.Added)?.Entity;

            if (conflicting is null)
                throw;

            throw new FraudCheckConflictException(conflicting.PaymentId, ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
