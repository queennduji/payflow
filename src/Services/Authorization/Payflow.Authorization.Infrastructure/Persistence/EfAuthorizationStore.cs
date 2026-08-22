using Microsoft.EntityFrameworkCore;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.Infrastructure.Persistence;

public sealed class EfAuthorizationStore(AuthorizationDbContext db) : IAuthorizationStore
{
    public Task<AuthorizationAttempt?> FindByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        db.AuthorizationAttempts.SingleOrDefaultAsync(a => a.PaymentId == paymentId, cancellationToken);

    public async Task SaveAsync(AuthorizationAttempt attempt, CancellationToken cancellationToken) =>
        await db.AuthorizationAttempts.AddAsync(attempt, cancellationToken);
}
