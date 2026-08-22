using Microsoft.EntityFrameworkCore;
using Payflow.Fraud.Application.Abstractions;
using Payflow.Fraud.Domain;

namespace Payflow.Fraud.Infrastructure.Persistence;

public sealed class EfFraudCheckRepository(FraudDbContext db) : IFraudCheckRepository
{
    public Task<FraudCheckRecord?> FindByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        db.FraudCheckRecords.SingleOrDefaultAsync(r => r.PaymentId == paymentId, cancellationToken);

    public Task<int> CountRecentAttemptsAsync(string merchantId, DateTimeOffset since, CancellationToken cancellationToken) =>
        db.FraudCheckRecords.CountAsync(r => r.MerchantId == merchantId && r.CreatedAt >= since, cancellationToken);

    public async Task AddAsync(FraudCheckRecord record, CancellationToken cancellationToken) =>
        await db.FraudCheckRecords.AddAsync(record, cancellationToken);
}
