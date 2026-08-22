using Microsoft.EntityFrameworkCore;
using Npgsql;
using Payflow.Ledger.Application.Abstractions;
using Payflow.Ledger.Domain;

namespace Payflow.Ledger.Infrastructure.Persistence;

public sealed class EfUnitOfWork(LedgerDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            var conflicting = db.ChangeTracker.Entries<LedgerEntryGroup>()
                .FirstOrDefault(e => e.State == EntityState.Added)?.Entity;

            if (conflicting is null)
                throw;

            throw new LedgerPostConflictException(conflicting.PaymentId, ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
