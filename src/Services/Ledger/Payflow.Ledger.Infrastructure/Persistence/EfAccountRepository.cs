using Microsoft.EntityFrameworkCore;
using Payflow.Ledger.Application.Abstractions;
using Payflow.Ledger.Domain;

namespace Payflow.Ledger.Infrastructure.Persistence;

public sealed class EfAccountRepository(LedgerDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(string accountId, CancellationToken cancellationToken) =>
        db.Accounts.SingleOrDefaultAsync(a => a.Id == accountId, cancellationToken);

    public async Task<Account> GetOrOpenAsync(string accountId, string currency, CancellationToken cancellationToken)
    {
        var existing = await db.Accounts.SingleOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (existing is not null)
            return existing;

        var account = Account.Open(accountId, accountId, InferType(accountId), currency);
        await db.Accounts.AddAsync(account, cancellationToken);
        return account;
    }

    public async Task<IReadOnlyList<(LedgerDirection Direction, decimal Amount)>> GetLinesForAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        var rows = await db.LedgerEntryGroups
            .SelectMany(g => g.Lines)
            .Where(l => l.AccountId == accountId)
            .Select(l => new { l.Direction, Amount = l.Amount.Amount })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Direction, r.Amount)).ToList();
    }

    // Simplified provisioning convention for the demo – see the remarks on Payflow.Ledger.Domain.Account.
    private static AccountType InferType(string accountId) => accountId switch
    {
        _ when accountId.StartsWith("merchant:", StringComparison.OrdinalIgnoreCase) => AccountType.Liability,
        _ when accountId.StartsWith("customer:", StringComparison.OrdinalIgnoreCase) => AccountType.Asset,
        _ => AccountType.Asset
    };
}
