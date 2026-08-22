using MediatR;
using Payflow.Fraud.Application.Abstractions;
using Payflow.Fraud.Domain;
using Payflow.Shared.Kernel;

namespace Payflow.Fraud.Application.FraudChecks;

/// <summary>
/// Idempotent per PaymentId (the saga could redeliver <c>CheckFraud</c> after a consumer crash).
/// Evaluates the amount/blocklist rule first since it needs no I/O, then velocity (which does).
/// </summary>
public sealed class EvaluateFraudCommandHandler(IFraudCheckRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<EvaluateFraudCommand, Result<FraudCheckResult>>
{
    public async Task<Result<FraudCheckResult>> Handle(EvaluateFraudCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByPaymentIdAsync(request.PaymentId, cancellationToken);
        if (existing is not null)
            return Result.Success(new FraudCheckResult(existing.Approved, existing.Reason));

        var (approved, reason) = FraudRules.EvaluateStatic(request.Amount, request.PaymentMethodRef);

        if (approved)
        {
            var since = DateTimeOffset.UtcNow - FraudRules.VelocityWindow;
            var recentAttempts = await repository.CountRecentAttemptsAsync(request.MerchantId, since, cancellationToken);
            (approved, reason) = FraudRules.EvaluateVelocity(recentAttempts);
        }

        var record = FraudCheckRecord.Record(request.PaymentId, request.MerchantId, request.Amount, request.Currency, approved, reason);
        await repository.AddAsync(record, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (FraudCheckConflictException)
        {
            var winner = await repository.FindByPaymentIdAsync(request.PaymentId, cancellationToken)
                ?? throw new InvalidOperationException("Lost a fraud-check race but the winning record is missing.");
            return Result.Success(new FraudCheckResult(winner.Approved, winner.Reason));
        }

        return Result.Success(new FraudCheckResult(approved, reason));
    }
}
