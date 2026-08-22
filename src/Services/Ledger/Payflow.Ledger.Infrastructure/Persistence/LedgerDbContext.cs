using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Ledger.Domain;

namespace Payflow.Ledger.Infrastructure.Persistence;

public sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LedgerEntryGroup> LedgerEntryGroups => Set<LedgerEntryGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
