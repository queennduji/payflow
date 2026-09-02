using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Payflow.Payments.Application.Payments;
using Payflow.Shared.Contracts.Messages;

namespace Payflow.Payments.Application.Saga;

/// <summary>
/// Orchestrates a payment through Fraud → Authorization → Ledger, with a compensating
/// <see cref="VoidAuthorization"/> when Ledger posting fails after authorization already
/// succeeded. This is the Phase 2 replacement for the Phase 1 in-handler HTTP call chain – see
/// ADR-0002 (the gap this closes) and ADR-0005 (this design).
///
/// The saga also plays a role in Payments.Api's synchronous facade: <see cref="ProcessPayment"/>
/// arrives via <c>IRequestClient</c>, so <c>Initially()</c> stashes the caller's
/// <see cref="ConsumeContext.ResponseAddress"/>/<see cref="ConsumeContext.RequestId"/> in saga
/// state. Each terminal branch publishes <see cref="PaymentOutcomeReady"/> rather than sending the
/// actual response itself – see ADR-0006 for why: a direct send here would leave via the transport
/// before this activity chain's own database writes (which run inside the saga repository's ambient
/// transaction) are guaranteed to have committed. Publishing goes through the same transactional
/// outbox as every other message this saga sends, so PaymentOutcomeReady is only ever observed
/// after the write that produced it is durable – the dedicated consumer of that message (see
/// Payflow.Payments.Api.Consumers.PaymentOutcomeReadyConsumer) is what actually responds.
/// </summary>
public sealed class PaymentSagaStateMachine : MassTransitStateMachine<PaymentSagaState>
{
    public State CheckingFraud { get; private set; } = null!;
    public State Authorizing { get; private set; } = null!;
    public State PostingLedger { get; private set; } = null!;
    public State VoidingAuthorization { get; private set; } = null!;

    public Event<ProcessPayment> ProcessPaymentRequested { get; private set; } = null!;
    public Event<FraudCheckPassed> FraudCheckPassed { get; private set; } = null!;
    public Event<FraudCheckFailed> FraudCheckFailed { get; private set; } = null!;
    public Event<PaymentAuthorized> PaymentAuthorized { get; private set; } = null!;
    public Event<PaymentAuthorizationDeclined> PaymentAuthorizationDeclined { get; private set; } = null!;
    public Event<LedgerEntryPosted> LedgerEntryPosted { get; private set; } = null!;
    public Event<LedgerPostFailed> LedgerPostFailed { get; private set; } = null!;
    public Event<AuthorizationVoided> AuthorizationVoided { get; private set; } = null!;

    public PaymentSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => ProcessPaymentRequested, x => x.CorrelateById(m => m.Message.PaymentId));
        Event(() => FraudCheckPassed, x => x.CorrelateById(m => m.Message.PaymentId));
        Event(() => FraudCheckFailed, x => x.CorrelateById(m => m.Message.PaymentId));
        Event(() => PaymentAuthorized, x => x.CorrelateById(m => m.Message.PaymentId));
        Event(() => PaymentAuthorizationDeclined, x => x.CorrelateById(m => m.Message.PaymentId));
        Event(() => LedgerEntryPosted, x => x.CorrelateById(m => m.Message.PaymentId));
        Event(() => LedgerPostFailed, x => x.CorrelateById(m => m.Message.PaymentId));
        Event(() => AuthorizationVoided, x => x.CorrelateById(m => m.Message.PaymentId));

        Initially(
            When(ProcessPaymentRequested)
                .Then(context =>
                {
                    context.Saga.MerchantId = context.Message.MerchantId;
                    context.Saga.Amount = context.Message.Amount;
                    context.Saga.Currency = context.Message.Currency;
                    context.Saga.PaymentMethodRef = context.Message.PaymentMethodRef;
                    context.Saga.CreatedAt = DateTimeOffset.UtcNow;
                    context.Saga.ResponseAddress = context.ResponseAddress;
                    context.Saga.RequestId = context.RequestId;
                })
                .Publish(context => new CheckFraud(
                    context.Saga.CorrelationId, context.Saga.MerchantId, context.Saga.Amount, context.Saga.Currency, context.Saga.PaymentMethodRef))
                .TransitionTo(CheckingFraud)
        );

        During(CheckingFraud,
            When(FraudCheckPassed)
                .Publish(context => new AuthorizePayment(
                    context.Saga.CorrelationId, context.Saga.Amount, context.Saga.Currency, context.Saga.PaymentMethodRef))
                .TransitionTo(Authorizing),

            When(FraudCheckFailed)
                .ThenAsync(context => MarkDeclinedAsync(context, context.Message.Reason))
                .Publish(context => OutcomeReady(context, "Declined", context.Message.Reason))
                .Publish(context => new SendPaymentNotification(context.Saga.CorrelationId, context.Saga.MerchantId, "Declined"))
                .Finalize()
        );

        During(Authorizing,
            When(PaymentAuthorized)
                .Then(context => context.Saga.AuthorizationId = context.Message.AuthorizationId)
                .ThenAsync(context => MarkAuthorizedAsync(context, context.Message.AuthorizationId))
                .Publish(context => new PostLedgerEntry(
                    context.Saga.CorrelationId,
                    $"customer:{context.Saga.PaymentMethodRef}",
                    $"merchant:{context.Saga.MerchantId}",
                    context.Saga.Amount,
                    context.Saga.Currency))
                .TransitionTo(PostingLedger),

            When(PaymentAuthorizationDeclined)
                .ThenAsync(context => MarkDeclinedAsync(context, context.Message.Reason))
                .Publish(context => OutcomeReady(context, "Declined", context.Message.Reason))
                .Publish(context => new SendPaymentNotification(context.Saga.CorrelationId, context.Saga.MerchantId, "Declined"))
                .Finalize()
        );

        During(PostingLedger,
            When(LedgerEntryPosted)
                .ThenAsync(context => MarkCapturedAsync(context))
                .Publish(context => OutcomeReady(context, "Captured", null))
                .Publish(context => new SendPaymentNotification(context.Saga.CorrelationId, context.Saga.MerchantId, "Captured"))
                .Finalize(),

            // Compensating transaction: authorization already succeeded, so it must be voided
            // before this payment can be reported as failed – see ADR-0005.
            When(LedgerPostFailed)
                .Then(context => context.Saga.PendingFailureReason = context.Message.Reason)
                .Publish(context => new VoidAuthorization(context.Saga.CorrelationId, context.Saga.AuthorizationId!.Value))
                .TransitionTo(VoidingAuthorization)
        );

        During(VoidingAuthorization,
            When(AuthorizationVoided)
                .ThenAsync(context => MarkFailedAsync(context, context.Saga.PendingFailureReason ?? "ledger-post-failed"))
                .Publish(context => OutcomeReady(context, "Failed", context.Saga.PendingFailureReason))
                .Publish(context => new SendPaymentNotification(context.Saga.CorrelationId, context.Saga.MerchantId, "Failed"))
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }

    private static Task MarkAuthorizedAsync(BehaviorContext<PaymentSagaState> context, Guid authorizationId) =>
        Sender(context).Send(new MarkPaymentAuthorizedCommand(context.Saga.CorrelationId, authorizationId));

    private static Task MarkDeclinedAsync(BehaviorContext<PaymentSagaState> context, string reason) =>
        Sender(context).Send(new MarkPaymentDeclinedCommand(context.Saga.CorrelationId, reason));

    private static Task MarkCapturedAsync(BehaviorContext<PaymentSagaState> context) =>
        Sender(context).Send(new MarkPaymentCapturedCommand(context.Saga.CorrelationId));

    private static Task MarkFailedAsync(BehaviorContext<PaymentSagaState> context, string reason) =>
        Sender(context).Send(new MarkPaymentFailedCommand(context.Saga.CorrelationId, reason));

    private static ISender Sender(BehaviorContext<PaymentSagaState> context)
    {
        if (!context.TryGetPayload<IServiceProvider>(out var provider))
            throw new InvalidOperationException("No IServiceProvider payload found on the saga's behavior context.");

        return provider.GetRequiredService<ISender>();
    }

    private static PaymentOutcomeReady OutcomeReady(BehaviorContext<PaymentSagaState> context, string status, string? failureReason) =>
        new(context.Saga.CorrelationId, status, failureReason, context.Saga.ResponseAddress, context.Saga.RequestId);
}
