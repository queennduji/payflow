using Payflow.Vault.Domain;

namespace Payflow.Vault.Application.Abstractions;

public interface IVaultTokenRepository
{
    Task AddAsync(VaultToken token, CancellationToken cancellationToken);
}
