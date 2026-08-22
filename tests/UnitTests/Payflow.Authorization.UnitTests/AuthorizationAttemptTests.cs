using FluentAssertions;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.UnitTests;

public class AuthorizationAttemptTests
{
    [Fact]
    public void Void_succeeds_on_an_approved_attempt()
    {
        var attempt = AuthorizationAttempt.Approve(Guid.NewGuid(), "AUTH-1");

        var result = attempt.Void();

        result.IsSuccess.Should().BeTrue();
        attempt.IsVoided.Should().BeTrue();
        attempt.VoidedAt.Should().NotBeNull();
    }

    [Fact]
    public void Void_is_idempotent_for_an_already_voided_attempt()
    {
        var attempt = AuthorizationAttempt.Approve(Guid.NewGuid(), "AUTH-1");
        attempt.Void();
        var voidedAt = attempt.VoidedAt;

        var result = attempt.Void();

        result.IsSuccess.Should().BeTrue();
        attempt.VoidedAt.Should().Be(voidedAt);
    }

    [Fact]
    public void Void_fails_on_a_declined_attempt()
    {
        var attempt = AuthorizationAttempt.Decline(Guid.NewGuid(), "insufficient_funds", "AUTH-1");

        var result = attempt.Void();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.CannotVoidDeclined");
    }
}
