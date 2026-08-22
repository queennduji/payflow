using FluentAssertions;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.UnitTests;

public class AuthorizationDecisionEngineTests
{
    [Fact]
    public void Approves_a_normal_charge()
    {
        var (approved, reason) = AuthorizationDecisionEngine.Decide(100m, "USD", "tok_visa");

        approved.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public void Declines_the_magic_test_token_regardless_of_amount()
    {
        var (approved, reason) = AuthorizationDecisionEngine.Decide(1m, "USD", AuthorizationDecisionEngine.AlwaysDeclineToken);

        approved.Should().BeFalse();
        reason.Should().Be("insufficient_funds");
    }

    [Fact]
    public void Declines_amounts_over_the_authorization_limit()
    {
        var (approved, reason) = AuthorizationDecisionEngine.Decide(10_000.01m, "USD", "tok_visa");

        approved.Should().BeFalse();
        reason.Should().Be("amount_exceeds_limit");
    }

    [Fact]
    public void Approves_exactly_at_the_authorization_limit()
    {
        var (approved, _) = AuthorizationDecisionEngine.Decide(10_000m, "USD", "tok_visa");

        approved.Should().BeTrue();
    }
}
