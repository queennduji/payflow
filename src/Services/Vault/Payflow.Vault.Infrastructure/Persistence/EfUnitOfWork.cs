using Payflow.Vault.Application.Abstractions;

namespace Payflow.Vault.Infrastructure.Persistence;

public sealed class EfUnitOfWork(VaultDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
