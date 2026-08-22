using MediatR;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Domain;
using Payflow.Shared.Kernel;

namespace Payflow.Authorization.Application.Authorizations;

public sealed class AuthorizePaymentCommandHandler(IAuthorizationStore store, IUnitOfWork unitOfWork)
    : IRequestHandler<AuthorizePaymentCommand, Result<AuthorizationResult>>
{
    public async Task<Result<AuthorizationResult>> Handle(AuthorizePaymentCommand request, CancellationToken cancellationToken)
    {
        var existing = await store.FindByPaymentIdAsync(request.PaymentId, cancellationToken);
        if (existing is not null)
            return Result.Success(ToResult(existing));

        var (approved, declineReason) = AuthorizationDecisionEngine.Decide(request.Amount, request.Currency, request.PaymentMethodRef);
        var processorReference = $"AUTH-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        var attempt = approved
            ? AuthorizationAttempt.Approve(request.PaymentId, processorReference)
            : AuthorizationAttempt.Decline(request.PaymentId, declineReason!, processorReference);

        await store.SaveAsync(attempt, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (AuthorizationConflictException)
        {
            var winner = await store.FindByPaymentIdAsync(request.PaymentId, cancellationToken)
                ?? throw new InvalidOperationException("Lost an authorization race but the winning record is missing.");
            return Result.Success(ToResult(winner));
        }

        return Result.Success(ToResult(attempt));
    }

    private static AuthorizationResult ToResult(AuthorizationAttempt attempt) =>
        new(attempt.Id, attempt.Approved, attempt.DeclineReason, attempt.ProcessorReference);
}
