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
/// <see cref="PaymentOutcomeReady"/> goes through the same transactional outbox as every other
/// message the saga publishes, specifically so this consumer only ever runs after the status change
/// that produced it is durable. In practice, MassTransit's EF outbox "deliver immediately after
/// SaveChanges" fast path can still fire slightly ahead of the *outer* commit that
/// <c>EntityFrameworkSagaRepositoryContextFactory</c> wraps saga processing in (observed directly:
/// a fresh, untracked read immediately after this message arrives occasionally still returns the
/// pre-transition status). Rather than depend on exact ordering between two different frameworks'
/// commit/dispatch internals, this consumer confirms the write is actually visible — a handful of
/// short polls, milliseconds in the overwhelmingly common case — before answering the caller. This
/// is a deliberate consistency guard, not a workaround for a bug in our own code.
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
