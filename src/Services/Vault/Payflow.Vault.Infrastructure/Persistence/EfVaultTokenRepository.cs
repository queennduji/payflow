using Payflow.Vault.Application.Abstractions;
using Payflow.Vault.Domain;

namespace Payflow.Vault.Infrastructure.Persistence;

public sealed class EfVaultTokenRepository(VaultDbContext db) : IVaultTokenRepository
{
    public async Task AddAsync(VaultToken token, CancellationToken cancellationToken) =>
        await db.VaultTokens.AddAsync(token, cancellationToken);
}
