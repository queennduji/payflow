using Payflow.Shared.Kernel;

namespace Payflow.Authorization.Domain;

/// <summary>Record of a single authorization decision made for a payment.</summary>
public sealed class AuthorizationAttempt : Entity<Guid>
{
    public Guid PaymentId { get; private set; }
    public bool Approved { get; private set; }
    public string? DeclineReason { get; private set; }
    public string ProcessorReference { get; private set; } = null!;
    public bool IsVoided { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AuthorizationAttempt() { }

    private AuthorizationAttempt(Guid id, Guid paymentId, bool approved, string? declineReason, string processorReference)
        : base(id)
    {
        PaymentId = paymentId;
        Approved = approved;
        DeclineReason = declineReason;
        ProcessorReference = processorReference;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static AuthorizationAttempt Approve(Guid paymentId, string processorReference) =>
        new(Guid.NewGuid(), paymentId, true, null, processorReference);

    public static AuthorizationAttempt Decline(Guid paymentId, string reason, string processorReference) =>
        new(Guid.NewGuid(), paymentId, false, reason, processorReference);

    /// <summary>
    /// The saga's compensating action when a later step (Ledger posting) fails after this
    /// authorization already succeeded. Idempotent – voiding an already-voided attempt is a no-op,
    /// since the command that triggers it can be redelivered.
    /// </summary>
    public Result Void()
    {
        if (!Approved)
            return Result.Failure(Error.Conflict("Authorization.CannotVoidDeclined", "Cannot void an authorization that was never approved."));

        if (IsVoided)
            return Result.Success();

        IsVoided = true;
        VoidedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }
}
