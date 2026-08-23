using Microsoft.EntityFrameworkCore;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Domain;

namespace Payflow.Payments.Infrastructure.Persistence;

public sealed class EfPaymentRepository(PaymentsDbContext db, IDbContextFactory<PaymentsDbContext> contextFactory) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken) =>
        await db.Payments.AddAsync(payment, cancellationToken);

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Payments.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payment?> GetByMerchantAndIdempotencyKeyAsync(string merchantId, string idempotencyKey, CancellationToken cancellationToken) =>
        db.Payments.SingleOrDefaultAsync(p => p.MerchantId == merchantId && p.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task ReloadAsync(Payment payment, CancellationToken cancellationToken)
    {
        // A plain db.Entry(payment).ReloadAsync() reuses this request's own long-lived connection,
        // which under Npgsql/EF Core can hand back a snapshot pinned to before the saga's writes —
        // observed in practice as this scope refusing to see an update a completely separate
        // connection (or a fresh DbContext) sees immediately. Reading through a brand-new context
        // sidesteps that ambiguity entirely rather than relying on undocumented connection reuse
        // semantics.
        await using var freshContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var fresh = await freshContext.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == payment.Id, cancellationToken);
        if (fresh is not null)
            db.Entry(payment).CurrentValues.SetValues(fresh);
    }
}
