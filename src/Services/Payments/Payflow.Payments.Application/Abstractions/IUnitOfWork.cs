namespace Payflow.Payments.Application.Abstractions;

/// <summary>Commits everything tracked in the current request's persistence context in one transaction.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
