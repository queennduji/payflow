using FluentAssertions;
using NSubstitute;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Application.Authorizations;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.UnitTests;

public class AuthorizePaymentCommandHandlerTests
{
    private readonly IAuthorizationStore _store = Substitute.For<IAuthorizationStore>();

    [Fact]
    public async Task A_repeated_call_for_the_same_payment_replays_the_original_decision_instead_of_deciding_again()
    {
        var paymentId = Guid.NewGuid();
        var original = AuthorizationAttempt.Approve(paymentId, "AUTH-ORIGINAL");
        _store.FindByPaymentIdAsync(paymentId, Arg.Any<CancellationToken>()).Returns(original);

        var handler = new AuthorizePaymentCommandHandler(_store);
        var result = await handler.Handle(new AuthorizePaymentCommand(paymentId, 50m, "USD", "tok_visa"), CancellationToken.None);

        result.Value.ProcessorReference.Should().Be("AUTH-ORIGINAL");
        await _store.DidNotReceive().SaveAsync(Arg.Any<AuthorizationAttempt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_new_payment_is_decided_and_saved()
    {
        _store.FindByPaymentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AuthorizationAttempt?)null);

        var handler = new AuthorizePaymentCommandHandler(_store);
        var result = await handler.Handle(new AuthorizePaymentCommand(Guid.NewGuid(), 50m, "USD", "tok_visa"), CancellationToken.None);

        result.Value.Approved.Should().BeTrue();
        await _store.Received(1).SaveAsync(Arg.Any<AuthorizationAttempt>(), Arg.Any<CancellationToken>());
    }
}
