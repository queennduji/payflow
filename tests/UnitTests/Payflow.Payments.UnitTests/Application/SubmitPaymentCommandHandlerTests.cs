using FluentAssertions;
using MassTransit;
using NSubstitute;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Application.Payments;
using Payflow.Payments.Domain;
using Payflow.Shared.Contracts.Messages;
using Payflow.Shared.Kernel;

namespace Payflow.Payments.UnitTests.Application;

public class SubmitPaymentCommandHandlerTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IIdempotencyStore _idempotency = Substitute.For<IIdempotencyStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRequestClient<ProcessPayment> _requestClient = Substitute.For<IRequestClient<ProcessPayment>>();

    private SubmitPaymentCommandHandler CreateHandler() => new(_payments, _idempotency, _unitOfWork, _requestClient);

    private static SubmitPaymentCommand ValidCommand() => new("merchant-1", 100m, "USD", "tok_visa", "idem-key-1");

    private void RequestClientReturns(PaymentProcessed response)
    {
        var typedResponse = Substitute.For<Response<PaymentProcessed>>();
        typedResponse.Message.Returns(response);
        _requestClient.GetResponse<PaymentProcessed>(Arg.Any<ProcessPayment>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns(typedResponse);
    }

    [Fact]
    public async Task Invalid_amount_fails_fast_without_creating_a_payment_or_starting_the_saga()
    {
        var result = await CreateHandler().Handle(ValidCommand() with { Amount = -5m }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _payments.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _requestClient.DidNotReceive().GetResponse<PaymentProcessed>(Arg.Any<ProcessPayment>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
    }

    [Fact]
    public async Task Replays_the_cached_response_without_starting_a_new_saga_when_the_key_was_already_used()
    {
        var cachedBody = """{"paymentId":"11111111-1111-1111-1111-111111111111","merchantId":"merchant-1","amount":100,"currency":"USD","status":"Captured","authorizationId":null,"failureReason":null,"createdAt":"2026-01-01T00:00:00+00:00"}""";
        var existing = IdempotencyRecord.Create("merchant-1", "idem-key-1", Guid.NewGuid(), 201, cachedBody);
        _idempotency.FindAsync("merchant-1", "idem-key-1", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var outcome = result.Value.Should().BeOfType<SubmitPaymentCompleted>().Subject;
        outcome.Payment.Status.Should().Be("Captured");
        await _requestClient.DidNotReceive().GetResponse<PaymentProcessed>(Arg.Any<ProcessPayment>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
    }

    [Fact]
    public async Task An_in_flight_race_on_a_terminal_payment_replays_its_result()
    {
        var winner = Payment.Submit("merchant-1", Money.Create(100m, "USD").Value, "tok_visa", "idem-key-1").Value;
        winner.Authorize(Guid.NewGuid());
        winner.Capture();

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new PaymentAlreadyInFlightException("merchant-1", "idem-key-1", new Exception("conflict")));
        _payments.GetByMerchantAndIdempotencyKeyAsync("merchant-1", "idem-key-1", Arg.Any<CancellationToken>()).Returns(winner);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        var outcome = result.Value.Should().BeOfType<SubmitPaymentCompleted>().Subject;
        outcome.Payment.Status.Should().Be("Captured");
        await _requestClient.DidNotReceive().GetResponse<PaymentProcessed>(Arg.Any<ProcessPayment>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
    }

    [Fact]
    public async Task An_in_flight_race_on_a_pending_payment_reports_still_processing_instead_of_starting_a_second_authorization()
    {
        var inFlight = Payment.Submit("merchant-1", Money.Create(100m, "USD").Value, "tok_visa", "idem-key-1").Value;

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new PaymentAlreadyInFlightException("merchant-1", "idem-key-1", new Exception("conflict")));
        _payments.GetByMerchantAndIdempotencyKeyAsync("merchant-1", "idem-key-1", Arg.Any<CancellationToken>()).Returns(inFlight);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        var outcome = result.Value.Should().BeOfType<SubmitPaymentPending>().Subject;
        outcome.PaymentId.Should().Be(inFlight.Id);
        await _requestClient.DidNotReceive().GetResponse<PaymentProcessed>(Arg.Any<ProcessPayment>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>());
    }

    [Fact]
    public async Task A_saga_timeout_reports_still_processing_rather_than_blocking_forever()
    {
        _requestClient.GetResponse<PaymentProcessed>(Arg.Any<ProcessPayment>(), Arg.Any<CancellationToken>(), Arg.Any<RequestTimeout>())
            .Returns<Task<Response<PaymentProcessed>>>(_ => throw new RequestTimeoutException());

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Value.Should().BeOfType<SubmitPaymentPending>();
    }

    [Fact]
    public async Task A_completed_saga_result_is_cached_when_the_outcome_is_captured()
    {
        var finalPayment = Payment.Submit("merchant-1", Money.Create(100m, "USD").Value, "tok_visa", "idem-key-1").Value;
        finalPayment.Authorize(Guid.NewGuid());
        finalPayment.Capture();
        _payments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(finalPayment);
        RequestClientReturns(new PaymentProcessed(finalPayment.Id, "Captured", null));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Value.Should().BeOfType<SubmitPaymentCompleted>();
        await _idempotency.Received(1).SaveAsync(Arg.Any<IdempotencyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_completed_saga_result_is_not_cached_when_the_outcome_is_failed()
    {
        var finalPayment = Payment.Submit("merchant-1", Money.Create(100m, "USD").Value, "tok_visa", "idem-key-1").Value;
        finalPayment.Authorize(Guid.NewGuid());
        finalPayment.Fail("ledger-post-failed");
        _payments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(finalPayment);
        RequestClientReturns(new PaymentProcessed(finalPayment.Id, "Failed", "ledger-post-failed"));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Value.Should().BeOfType<SubmitPaymentCompleted>();
        await _idempotency.DidNotReceive().SaveAsync(Arg.Any<IdempotencyRecord>(), Arg.Any<CancellationToken>());
    }
}
