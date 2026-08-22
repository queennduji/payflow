using FluentAssertions;
using Payflow.Fraud.Domain;

namespace Payflow.Fraud.UnitTests;

public class FraudRulesTests
{
    [Fact]
    public void Approves_a_normal_charge()
    {
        var (approved, reason) = FraudRules.EvaluateStatic(100m, "tok_visa");

        approved.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public void Rejects_the_blocked_test_token_regardless_of_amount()
    {
        var (approved, reason) = FraudRules.EvaluateStatic(1m, FraudRules.BlockedToken);

        approved.Should().BeFalse();
        reason.Should().Be("blocked_payment_method");
    }

    [Fact]
    public void Rejects_amounts_over_the_high_risk_threshold()
    {
        var (approved, reason) = FraudRules.EvaluateStatic(5_000.01m, "tok_visa");

        approved.Should().BeFalse();
        reason.Should().Be("amount_high_risk");
    }

    [Fact]
    public void Velocity_check_approves_when_under_the_limit()
    {
        var (approved, reason) = FraudRules.EvaluateVelocity(FraudRules.MaxAttemptsPerWindow - 1);

        approved.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public void Velocity_check_rejects_at_the_limit()
    {
        var (approved, reason) = FraudRules.EvaluateVelocity(FraudRules.MaxAttemptsPerWindow);

        approved.Should().BeFalse();
        reason.Should().Be("velocity_limit_exceeded");
    }
}
