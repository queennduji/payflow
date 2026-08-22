using FluentAssertions;
using NSubstitute;
using Payflow.Notifications.Application.Abstractions;
using Payflow.Notifications.Application.Notifications;
using Payflow.Notifications.Domain;

namespace Payflow.Notifications.UnitTests;

public class SendNotificationCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SendNotificationCommandHandler CreateHandler() => new(_repository, _unitOfWork);

    [Fact]
    public async Task A_repeated_call_for_the_same_payment_replays_the_original_attempt_without_recording_twice()
    {
        var paymentId = Guid.NewGuid();
        var existing = NotificationAttempt.Record(paymentId, "merchant-1", "Captured");
        _repository.FindByPaymentIdAsync(paymentId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await CreateHandler().Handle(new SendNotificationCommand(paymentId, "merchant-1", "Captured"), CancellationToken.None);

        result.Value.Status.Should().Be("Captured");
        await _repository.DidNotReceive().AddAsync(Arg.Any<NotificationAttempt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_new_payment_is_recorded_as_sent()
    {
        _repository.FindByPaymentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((NotificationAttempt?)null);

        var result = await CreateHandler().Handle(new SendNotificationCommand(Guid.NewGuid(), "merchant-1", "Declined"), CancellationToken.None);

        result.Value.Status.Should().Be("Declined");
        await _repository.Received(1).AddAsync(Arg.Any<NotificationAttempt>(), Arg.Any<CancellationToken>());
    }
}
