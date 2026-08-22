using Microsoft.EntityFrameworkCore;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Domain;

namespace Payflow.Payments.Infrastructure.Persistence;

public sealed class EfIdempotencyStore(PaymentsDbContext db) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> FindAsync(string merchantId, string key, CancellationToken cancellationToken) =>
        db.IdempotencyRecords.SingleOrDefaultAsync(r => r.MerchantId == merchantId && r.Key == key, cancellationToken);

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken) =>
        await db.IdempotencyRecords.AddAsync(record, cancellationToken);
}
