using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payflow.Payments.Infrastructure.Persistence;
using Payflow.Shared.Contracts.Messages;

namespace Payflow.Payments.Api.Consumers;

/// <summary>
/// Bridges the saga's terminal outcome back to whoever is holding a pending
/// <c>IRequestClient&lt;ProcessPayment&gt;</c> await. A no-op if the original caller already gave
/// up and got a 202 instead — there's nothing stored to respond to in that case.
/// </summary>
/// <remarks>
/// The EF outbox's "dispatch immediately after SaveChanges" fast path can beat the *outer*
/// transaction that <c>EntityFrameworkSagaRepositoryContextFactory</c> wraps saga processing in, so
/// this message can arrive slightly ahead of its own status change being visible to a fresh read.
/// Rather than rely on exact ordering between two frameworks' commit/dispatch internals, this
/// consumer polls (briefly — milliseconds in practice) until the write is visible before replying.
/// </remarks>
public sealed class PaymentOutcomeReadyConsumer(IDbContextFactory<PaymentsDbContext> contextFactory) : IConsumer<PaymentOutcomeReady>
{
    private const int MaxAttempts = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    public async Task Consume(ConsumeContext<PaymentOutcomeReady> context)
    {
        var message = context.Message;
        if (message.ResponseAddress is null || message.RequestId is null)
            return;

        await WaitUntilVisibleAsync(message.PaymentId, message.Status, context.CancellationToken);

        var endpoint = await context.GetSendEndpoint(message.ResponseAddress);
        await endpoint.Send(new PaymentProcessed(message.PaymentId, message.Status, message.FailureReason), sendContext =>
        {
            sendContext.RequestId = message.RequestId;
        });
    }

    private async Task WaitUntilVisibleAsync(Guid paymentId, string expectedStatus, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var status = await db.Payments.AsNoTracking()
                .Where(p => p.Id == paymentId)
                .Select(p => p.Status)
                .SingleOrDefaultAsync(cancellationToken);

            if (status.ToString() == expectedStatus)
                return;

            await Task.Delay(PollInterval, cancellationToken);
        }
    }
}
