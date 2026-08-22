using Microsoft.EntityFrameworkCore;
using Npgsql;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.Infrastructure.Persistence;

public sealed class EfUnitOfWork(AuthorizationDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            var conflicting = db.ChangeTracker.Entries<AuthorizationAttempt>()
                .FirstOrDefault(e => e.State == EntityState.Added)?.Entity;

            if (conflicting is null)
                throw;

            throw new AuthorizationConflictException(conflicting.PaymentId, ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
