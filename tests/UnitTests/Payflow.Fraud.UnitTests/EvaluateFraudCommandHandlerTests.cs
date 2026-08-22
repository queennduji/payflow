using FluentAssertions;
using NSubstitute;
using Payflow.Fraud.Application.Abstractions;
using Payflow.Fraud.Application.FraudChecks;
using Payflow.Fraud.Domain;

namespace Payflow.Fraud.UnitTests;

public class EvaluateFraudCommandHandlerTests
{
    private readonly IFraudCheckRepository _repository = Substitute.For<IFraudCheckRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private EvaluateFraudCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task A_repeated_call_for_the_same_payment_replays_the_original_decision()
    {
        var paymentId = Guid.NewGuid();
        var existing = FraudCheckRecord.Record(paymentId, "merchant-1", 50m, "USD", approved: true, reason: null);
        _repository.FindByPaymentIdAsync(paymentId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await CreateHandler().Handle(new EvaluateFraudCommand(paymentId, "merchant-1", 50m, "USD", "tok_visa"), CancellationToken.None);

        result.Value.Approved.Should().BeTrue();
        await _repository.DidNotReceive().CountRecentAttemptsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Blocked_token_is_rejected_without_checking_velocity()
    {
        var result = await CreateHandler().Handle(
            new EvaluateFraudCommand(Guid.NewGuid(), "merchant-1", 10m, "USD", FraudRules.BlockedToken), CancellationToken.None);

        result.Value.Approved.Should().BeFalse();
        result.Value.Reason.Should().Be("blocked_payment_method");
        await _repository.DidNotReceive().CountRecentAttemptsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Too_many_recent_attempts_fails_the_velocity_check()
    {
        _repository.CountRecentAttemptsAsync("merchant-1", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(FraudRules.MaxAttemptsPerWindow);

        var result = await CreateHandler().Handle(
            new EvaluateFraudCommand(Guid.NewGuid(), "merchant-1", 10m, "USD", "tok_visa"), CancellationToken.None);

        result.Value.Approved.Should().BeFalse();
        result.Value.Reason.Should().Be("velocity_limit_exceeded");
    }

    [Fact]
    public async Task A_new_payment_within_limits_is_approved_and_recorded()
    {
        _repository.CountRecentAttemptsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(0);

        var result = await CreateHandler().Handle(
            new EvaluateFraudCommand(Guid.NewGuid(), "merchant-1", 10m, "USD", "tok_visa"), CancellationToken.None);

        result.Value.Approved.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<FraudCheckRecord>(), Arg.Any<CancellationToken>());
    }
}
