using FluentAssertions;
using Payflow.Payments.Domain;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.UnitTests.Domain;

public class PaymentTests
{
    private static Payment NewPendingPayment(decimal amount = 100m) =>
        Payment.Submit("merchant-1", Money.Create(amount, "USD").Value, "tok_visa", "idem-key-1").Value;

    [Fact]
    public void Submit_creates_a_pending_payment_and_raises_PaymentSubmitted()
    {
        var payment = NewPendingPayment();

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentSubmitted);
    }

    [Fact]
    public void Authorize_transitions_pending_to_authorized()
    {
        var payment = NewPendingPayment();
        var authorizationId = Guid.NewGuid();

        var result = payment.Authorize(authorizationId);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Authorized);
        payment.AuthorizationId.Should().Be(authorizationId);
    }

    [Fact]
    public void Authorize_fails_when_payment_is_not_pending()
    {
        var payment = NewPendingPayment();
        payment.Authorize(Guid.NewGuid());

        var result = payment.Authorize(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Capture_requires_prior_authorization()
    {
        var payment = NewPendingPayment();

        var result = payment.Capture();

        result.IsFailure.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Full_happy_path_reaches_captured()
    {
        var payment = NewPendingPayment();

        payment.Authorize(Guid.NewGuid());
        var result = payment.Capture();

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public void Decline_is_terminal_and_cannot_be_captured_afterwards()
    {
        var payment = NewPendingPayment();

        payment.Decline("insufficient_funds");
        var result = payment.Authorize(Guid.NewGuid());

        payment.Status.Should().Be(PaymentStatus.Declined);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Fail_cannot_be_applied_to_an_already_captured_payment()
    {
        var payment = NewPendingPayment();
        payment.Authorize(Guid.NewGuid());
        payment.Capture();

        var result = payment.Fail("ledger-post-failed");

        result.IsFailure.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Captured);
    }
}
