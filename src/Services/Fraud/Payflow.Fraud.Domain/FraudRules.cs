namespace Payflow.Fraud.Domain;

/// <summary>
/// Pure decision rules, split into the part that needs no history (blocklist, amount threshold)
/// and the part that does (velocity – how many attempts a merchant has made recently). Application
/// owns fetching the recent-attempt count; this class stays a plain function of its inputs so both
/// halves are trivially unit-testable.
/// </summary>
public static class FraudRules
{
    /// <summary>Magic test token that always fails fraud review, mirroring Authorization's <c>tok_declined</c>.</summary>
    public const string BlockedToken = "tok_fraud";

    private const decimal HighRiskAmountThreshold = 5_000m;
    public const int MaxAttemptsPerWindow = 5;
    public static readonly TimeSpan VelocityWindow = TimeSpan.FromMinutes(1);

    public static (bool Approved, string? Reason) EvaluateStatic(decimal amount, string paymentMethodRef)
    {
        if (string.Equals(paymentMethodRef, BlockedToken, StringComparison.OrdinalIgnoreCase))
            return (false, "blocked_payment_method");

        if (amount > HighRiskAmountThreshold)
            return (false, "amount_high_risk");

        return (true, null);
    }

    public static (bool Approved, string? Reason) EvaluateVelocity(int recentAttemptCount) =>
        recentAttemptCount >= MaxAttemptsPerWindow
            ? (false, "velocity_limit_exceeded")
            : (true, null);
}
