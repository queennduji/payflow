using Payflow.Fraud.Domain;

namespace Payflow.Fraud.Application.Abstractions;

public interface IFraudCheckRepository
{
    Task<FraudCheckRecord?> FindByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<int> CountRecentAttemptsAsync(string merchantId, DateTimeOffset since, CancellationToken cancellationToken);
    Task AddAsync(FraudCheckRecord record, CancellationToken cancellationToken);
}
