using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Fraud.Domain;

namespace Payflow.Fraud.Infrastructure.Persistence;

public sealed class FraudDbContext(DbContextOptions<FraudDbContext> options) : DbContext(options)
{
    public DbSet<FraudCheckRecord> FraudCheckRecords => Set<FraudCheckRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FraudDbContext).Assembly);

        // MassTransit's transactional outbox: publishing a result event and recording the fraud
        // check happen in the same DB transaction, so a crash after commit can never lose the
        // outgoing message, and a crash before commit can never leak one that never happened.
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
