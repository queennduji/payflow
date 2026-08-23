using FluentAssertions;
using NSubstitute;
using Payflow.Authorization.Application.Abstractions;
using Payflow.Authorization.Application.Authorizations;
using Payflow.Authorization.Domain;

namespace Payflow.Authorization.UnitTests;

public class AuthorizePaymentCommandHandlerTests
{
    private readonly IAuthorizationStore _store = Substitute.For<IAuthorizationStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICardNetworkClient _cardNetwork = Substitute.For<ICardNetworkClient>();

    private AuthorizePaymentCommandHandler CreateHandler() => new(_store, _unitOfWork, _cardNetwork);

    [Fact]
    public async Task A_repeated_call_for_the_same_payment_replays_the_original_decision_instead_of_deciding_again()
    {
        var paymentId = Guid.NewGuid();
        var original = AuthorizationAttempt.Approve(paymentId, "AUTH-ORIGINAL");
        _store.FindByPaymentIdAsync(paymentId, Arg.Any<CancellationToken>()).Returns(original);

        var result = await CreateHandler().Handle(new AuthorizePaymentCommand(paymentId, 50m, "USD", "tok_visa"), CancellationToken.None);

        result.Value.ProcessorReference.Should().Be("AUTH-ORIGINAL");
        await _store.DidNotReceive().SaveAsync(Arg.Any<AuthorizationAttempt>(), Arg.Any<CancellationToken>());
        await _cardNetwork.DidNotReceive().AuthorizeAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_new_payment_is_decided_by_the_card_network_and_saved()
    {
        _store.FindByPaymentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AuthorizationAttempt?)null);
        _cardNetwork.AuthorizeAsync(50m, "USD", "tok_visa", Arg.Any<CancellationToken>())
            .Returns(new CardNetworkResult(true, null));

        var result = await CreateHandler().Handle(new AuthorizePaymentCommand(Guid.NewGuid(), 50m, "USD", "tok_visa"), CancellationToken.None);

        result.Value.Approved.Should().BeTrue();
        await _store.Received(1).SaveAsync(Arg.Any<AuthorizationAttempt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_card_network_decline_is_recorded_with_its_reason()
    {
        _store.FindByPaymentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AuthorizationAttempt?)null);
        _cardNetwork.AuthorizeAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CardNetworkResult(false, "processor_unavailable"));

        var result = await CreateHandler().Handle(new AuthorizePaymentCommand(Guid.NewGuid(), 50m, "USD", "tok_visa"), CancellationToken.None);

        result.Value.Approved.Should().BeFalse();
        result.Value.DeclineReason.Should().Be("processor_unavailable");
    }
}
