using System.Text.Json;
using MediatR;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Domain;
using Payflow.Shared.Contracts.Authorization;
using Payflow.Shared.Contracts.Ledger;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.Application.Payments;

/// <summary>
/// Orchestrates a single payment attempt: idempotency dedup, authorization, and ledger posting.
/// This is a synchronous, in-process orchestration for Phase 1. Phase 2 lifts the same steps into
/// a MassTransit saga so a crash between steps doesn't leave the payment stuck — see ADR-0002.
/// </summary>
public sealed class SubmitPaymentCommandHandler(
    IPaymentRepository payments,
    IIdempotencyStore idempotencyStore,
    IAuthorizationClient authorizationClient,
    ILedgerClient ledgerClient,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitPaymentCommand, Result<PaymentResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<PaymentResponse>> Handle(SubmitPaymentCommand request, CancellationToken cancellationToken)
    {
        var existing = await idempotencyStore.FindAsync(request.MerchantId, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var replayed = JsonSerializer.Deserialize<PaymentResponse>(existing.ResponseBody, JsonOptions)!;
            return Result.Success(replayed);
        }

        var moneyResult = Money.Create(request.Amount, request.Currency);
        if (moneyResult.IsFailure)
            return Result.Failure<PaymentResponse>(moneyResult.Error);

        var paymentResult = Payment.Submit(request.MerchantId, moneyResult.Value, request.PaymentMethodRef, request.IdempotencyKey);
        if (paymentResult.IsFailure)
            return Result.Failure<PaymentResponse>(paymentResult.Error);

        var payment = paymentResult.Value;
        await payments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var authorization = await authorizationClient.AuthorizeAsync(
            new AuthorizeRequest(payment.Id, payment.Amount.Amount, payment.Amount.Currency, payment.PaymentMethodRef),
            cancellationToken);

        if (!authorization.Approved)
        {
            payment.Decline(authorization.DeclineReason ?? "declined");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return await FinalizeAndCache(payment, cancellationToken, cacheable: true);
        }

        payment.Authorize(authorization.AuthorizationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var ledgerRequest = new PostLedgerEntryRequest(
            payment.Id,
            DebitAccountId: $"customer:{payment.PaymentMethodRef}",
            CreditAccountId: $"merchant:{payment.MerchantId}",
            payment.Amount.Amount,
            payment.Amount.Currency);

        var ledgerResponse = await ledgerClient.PostEntryAsync(ledgerRequest, cancellationToken);

        if (!ledgerResponse.Posted)
        {
            // Downstream/infra failure, not a business decline: don't cache the idempotency record
            // so a retry with the same key can still succeed once Ledger recovers.
            payment.Fail("ledger-post-failed");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(ToResponse(payment));
        }

        payment.Capture();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await FinalizeAndCache(payment, cancellationToken, cacheable: true);
    }

    private async Task<Result<PaymentResponse>> FinalizeAndCache(Payment payment, CancellationToken cancellationToken, bool cacheable)
    {
        var response = ToResponse(payment);

        if (cacheable)
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
                // A concurrent request for the same (merchant, key) won the race and committed
                // first. Its result is authoritative — replay it rather than surfacing a conflict.
                var winner = await idempotencyStore.FindAsync(payment.MerchantId, payment.IdempotencyKey, cancellationToken)
                    ?? throw new InvalidOperationException("Lost an idempotency race but the winning record is missing.");
                return Result.Success(JsonSerializer.Deserialize<PaymentResponse>(winner.ResponseBody, JsonOptions)!);
            }
        }

        return Result.Success(response);
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
