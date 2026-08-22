using Payflow.Payments.Domain;

namespace Payflow.Payments.Application.Abstractions;

/// <summary>Persistence port for the Payment aggregate. Implemented by Infrastructure (EF Core).</summary>
public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
