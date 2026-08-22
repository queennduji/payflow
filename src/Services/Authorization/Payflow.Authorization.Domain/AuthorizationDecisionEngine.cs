namespace Payflow.Authorization.Domain;

/// <summary>
/// Stands in for a call to a real card network. Deterministic and side-effect free so the
/// decision rule itself is trivially unit-testable; Phase 3 wraps the (still-mocked) network call
/// with configurable latency/fault injection for chaos testing without touching this rule.
/// </summary>
public static class AuthorizationDecisionEngine
{
    private const decimal AuthorizationLimit = 10_000m;

    /// <summary>The magic test token that always declines, mirroring how real processors' test modes work.</summary>
    public const string AlwaysDeclineToken = "tok_declined";

    public static (bool Approved, string? DeclineReason) Decide(decimal amount, string currency, string paymentMethodRef)
    {
        if (string.Equals(paymentMethodRef, AlwaysDeclineToken, StringComparison.OrdinalIgnoreCase))
            return (false, "insufficient_funds");

        if (amount > AuthorizationLimit)
            return (false, "amount_exceeds_limit");

        return (true, null);
    }
}
