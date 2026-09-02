namespace Payflow.Shared.Contracts.Messages;

/// <summary>
/// Published by the saga when it reaches a terminal state, instead of directly sending the
/// response to the original <see cref="ProcessPayment"/> requester. This message goes through the
/// normal transactional outbox (like every other message the saga publishes), so its delivery is
/// only ever observed after the payment status change that produced it has actually committed.
/// <see cref="PaymentProcessed"/> is only ever sent by the dedicated consumer of this message –
/// see ADR-0006 and Payflow.Payments.Api.Consumers.PaymentOutcomeReadyConsumer.
/// </summary>
public sealed record PaymentOutcomeReady(Guid PaymentId, string Status, string? FailureReason, Uri? ResponseAddress, Guid? RequestId);
