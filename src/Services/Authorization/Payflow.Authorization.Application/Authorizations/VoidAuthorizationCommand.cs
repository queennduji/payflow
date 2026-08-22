using MediatR;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Shared.Kernel;

namespace Payflow.Authorization.Application.Authorizations;

public sealed record VoidAuthorizationCommand(Guid PaymentId) : IRequest<Result>;

/// <summary>The Authorization side of the saga's compensating transaction (ADR-0005).</summary>
public sealed class VoidAuthorizationCommandHandler(IAuthorizationStore store, IUnitOfWork unitOfWork)
    : IRequestHandler<VoidAuthorizationCommand, Result>
{
    public async Task<Result> Handle(VoidAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var attempt = await store.FindByPaymentIdAsync(request.PaymentId, cancellationToken);
        if (attempt is null)
            return Result.Failure(Error.NotFound("Authorization.NotFound", $"No authorization recorded for payment '{request.PaymentId}'."));

        var voidResult = attempt.Void();
        if (voidResult.IsFailure)
            return voidResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
