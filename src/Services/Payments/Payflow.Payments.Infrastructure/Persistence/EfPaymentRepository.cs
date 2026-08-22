using Microsoft.EntityFrameworkCore;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Domain;

namespace Payflow.Payments.Infrastructure.Persistence;

public sealed class EfPaymentRepository(PaymentsDbContext db) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken) =>
        await db.Payments.AddAsync(payment, cancellationToken);

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Payments.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payment?> GetByMerchantAndIdempotencyKeyAsync(string merchantId, string idempotencyKey, CancellationToken cancellationToken) =>
        db.Payments.SingleOrDefaultAsync(p => p.MerchantId == merchantId && p.IdempotencyKey == idempotencyKey, cancellationToken);
}
