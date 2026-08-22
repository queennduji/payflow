using Payflow.Ledger.Domain;

namespace Payflow.Ledger.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken);

    /// <summary>Opens the account with a type inferred from its id prefix if it doesn't exist yet. See ADR-0003.</summary>
    Task<Account> GetOrOpenAsync(string accountId, string currency, CancellationToken cancellationToken);

    Task<IReadOnlyList<(LedgerDirection Direction, decimal Amount)>> GetLinesForAccountAsync(string accountId, CancellationToken cancellationToken);
}
