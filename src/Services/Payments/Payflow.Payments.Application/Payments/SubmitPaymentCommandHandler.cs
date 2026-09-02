using System.Text.Json;
using MassTransit;
using MediatR;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Domain;
using Payflow.Shared.Contracts.Messages;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.Application.Payments;

/// <summary>
/// Starts the payment saga and presents it synchronously to the caller when possible. Idempotency
/// dedup and the (merchant, key) race handling are unchanged from Phase 1; what changed is how the
/// actual authorize/capture work happens – see <see cref="Saga.PaymentSagaStateMachine"/>, ADR-0002,
/// and ADR-0006 for the request/response bridge this handler implements.
/// </summary>
public sealed class SubmitPaymentCommandHandler(
    IPaymentRepository payments,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IRequestClient<ProcessPayment> requestClient)
    : IRequestHandler<SubmitPaymentCommand, Result<SubmitPaymentOutcome>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan SagaResponseTimeout = TimeSpan.FromSeconds(10);

    public async Task<Result<SubmitPaymentOutcome>> Handle(SubmitPaymentCommand request, CancellationToken cancellationToken)
    {
        var existing = await idempotencyStore.FindAsync(request.MerchantId, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var replayed = JsonSerializer.Deserialize<PaymentResponse>(existing.ResponseBody, JsonOptions)!;
            return Result.Success<SubmitPaymentOutcome>(new SubmitPaymentCompleted(replayed));
        }

        var moneyResult = Money.Create(request.Amount, request.Currency);
        if (moneyResult.IsFailure)
            return Result.Failure<SubmitPaymentOutcome>(moneyResult.Error);

        var paymentResult = Payment.Submit(request.MerchantId, moneyResult.Value, request.PaymentMethodRef, request.IdempotencyKey);
        if (paymentResult.IsFailure)
            return Result.Failure<SubmitPaymentOutcome>(paymentResult.Error);

        var payment = paymentResult.Value;
        await payments.AddAsync(payment, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PaymentAlreadyInFlightException)
        {
            // Someone already has this (merchant, key) in flight – this is exactly the Phase-1 gap
            // ADR-0002 called out. Rather than starting a second authorization, report on the
            // existing attempt: replay it if it's already terminal, otherwise tell the caller to
            // poll rather than joining a wait we have no handle on.
            var inFlight = await payments.GetByMerchantAndIdempotencyKeyAsync(request.MerchantId, request.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("Lost a payment-creation race but the winning payment is missing.");

            return inFlight.Status is PaymentStatus.Pending or PaymentStatus.Authorized
                ? Result.Success<SubmitPaymentOutcome>(new SubmitPaymentPending(inFlight.Id))
                : Result.Success<SubmitPaymentOutcome>(new SubmitPaymentCompleted(ToResponse(inFlight)));
        }

        try
        {
            await requestClient.GetResponse<PaymentProcessed>(
                new ProcessPayment(payment.Id, payment.MerchantId, payment.Amount.Amount, payment.Amount.Currency, payment.PaymentMethodRef),
                cancellationToken,
                SagaResponseTimeout);

            // The saga updated this row from its own consumers, each in a different scope – reload
            // rather than re-query, or EF Core's identity map hands back this same stale tracked
            // instance instead of hitting the database.
            await payments.ReloadAsync(payment, cancellationToken);

            return await FinalizeAndCache(payment, cancellationToken);
        }
        catch (RequestTimeoutException)
        {
            // The saga is still running (Fraud/Authorization/Ledger haven't all answered yet). The
            // API contract falls back to async here rather than blocking indefinitely – see ADR-0006.
            return Result.Success<SubmitPaymentOutcome>(new SubmitPaymentPending(payment.Id));
        }
    }

    private async Task<Result<SubmitPaymentOutcome>> FinalizeAndCache(Payment payment, CancellationToken cancellationToken)
    {
        var response = ToResponse(payment);

        // Only cache a definitive business outcome. Failed means the saga's compensating
        // transaction already ran for a downstream/infra failure – not caching it lets a retry with
        // the same key try the whole flow again once things recover.
        if (payment.Status is PaymentStatus.Captured or PaymentStatus.Declined)
        {
            var body = JsonSerializer.Serialize(response, JsonOptions);
            var record = IdempotencyRecord.Create(payment.MerchantId, payment.IdempotencyKey, payment.Id, 201, body);
            await idempotencyStore.SaveAsync(record, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (IdempotencyKeyConflictException)
            {
                var winner = await idempotencyStore.FindAsync(payment.MerchantId, payment.IdempotencyKey, cancellationToken)
                    ?? throw new InvalidOperationException("Lost an idempotency race but the winning record is missing.");
                return Result.Success<SubmitPaymentOutcome>(
                    new SubmitPaymentCompleted(JsonSerializer.Deserialize<PaymentResponse>(winner.ResponseBody, JsonOptions)!));
            }
        }

        return Result.Success<SubmitPaymentOutcome>(new SubmitPaymentCompleted(response));
    }

    private static PaymentResponse ToResponse(Payment payment) => new(
        payment.Id,
        payment.MerchantId,
        payment.Amount.Amount,
        payment.Amount.Currency,
        payment.Status.ToString(),
        payment.AuthorizationId,
        payment.FailureReason,
        payment.CreatedAt);
}
