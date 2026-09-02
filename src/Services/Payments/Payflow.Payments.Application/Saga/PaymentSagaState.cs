using MassTransit;

namespace Payflow.Payments.Application.Saga;

/// <summary>
/// Durable saga state for one payment's journey through fraud check, authorization, and ledger
/// posting. <see cref="CorrelationId"/> is the PaymentId – every message in the flow correlates on
/// it. Persisted via MassTransit's EF Core saga repository against <c>PaymentsDbContext</c>, which
/// is what makes the orchestration survive a process crash mid-flow (the gap ADR-0002 documented).
/// </summary>
public sealed class PaymentSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    public string MerchantId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string PaymentMethodRef { get; set; } = null!;
    public Guid? AuthorizationId { get; set; }

    /// <summary>Stashed from the LedgerPostFailed event so VoidingAuthorization's completion can report the original reason.</summary>
    public string? PendingFailureReason { get; set; }

    /// <summary>Where to send <see cref="Payflow.Shared.Contracts.Messages.PaymentProcessed"/> once this saga finalizes – see ADR-0006.</summary>
    public Uri? ResponseAddress { get; set; }
    public Guid? RequestId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
