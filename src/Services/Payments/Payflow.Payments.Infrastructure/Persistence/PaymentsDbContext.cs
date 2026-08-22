using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Payments.Application.Saga;
using Payflow.Payments.Domain;

namespace Payflow.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<PaymentSagaState> PaymentSagaStates => Set<PaymentSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
