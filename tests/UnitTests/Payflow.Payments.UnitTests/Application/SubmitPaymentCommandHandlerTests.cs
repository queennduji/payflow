using FluentAssertions;
using NSubstitute;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Application.Payments;
using Payflow.Payments.Domain;
using Payflow.Shared.Contracts.Authorization;
using Payflow.Shared.Contracts.Ledger;

namespace Payflow.Payments.UnitTests.Application;

public class SubmitPaymentCommandHandlerTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IIdempotencyStore _idempotency = Substitute.For<IIdempotencyStore>();
    private readonly IAuthorizationClient _authorizationClient = Substitute.For<IAuthorizationClient>();
    private readonly ILedgerClient _ledgerClient = Substitute.For<ILedgerClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SubmitPaymentCommandHandler CreateHandler() =>
        new(_payments, _idempotency, _authorizationClient, _ledgerClient, _unitOfWork);

    private static SubmitPaymentCommand ValidCommand() =>
        new("merchant-1", 100m, "USD", "tok_visa", "idem-key-1");

    [Fact]
    public async Task Replays_the_cached_response_without_calling_downstream_services_when_key_was_already_used()
    {
        var cachedBody = """{"paymentId":"11111111-1111-1111-1111-111111111111","merchantId":"merchant-1","amount":100,"currency":"USD","status":"Captured","authorizationId":null,"failureReason":null,"createdAt":"2026-01-01T00:00:00+00:00"}""";
        var existing = IdempotencyRecord.Create("merchant-1", "idem-key-1", Guid.NewGuid(), 201, cachedBody);
        _idempotency.FindAsync("merchant-1", "idem-key-1", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Captured");
        await _authorizationClient.DidNotReceive().AuthorizeAsync(Arg.Any<AuthorizeRequest>(), Arg.Any<CancellationToken>());
        await _ledgerClient.DidNotReceive().PostEntryAsync(Arg.Any<PostLedgerEntryRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approved_authorization_and_successful_ledger_post_captures_the_payment_and_caches_the_result()
    {
        _authorizationClient.AuthorizeAsync(Arg.Any<AuthorizeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AuthorizeResponse(Guid.NewGuid(), Approved: true, DeclineReason: null, ProcessorReference: "AUTH-1"));
        _ledgerClient.PostEntryAsync(Arg.Any<PostLedgerEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PostLedgerEntryResponse(Guid.NewGuid(), Posted: true));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(PaymentStatus.Captured));
        await _idempotency.Received(1).SaveAsync(Arg.Any<IdempotencyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Declined_authorization_declines_the_payment_and_still_caches_the_result()
    {
        _authorizationClient.AuthorizeAsync(Arg.Any<AuthorizeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AuthorizeResponse(Guid.NewGuid(), Approved: false, DeclineReason: "insufficient_funds", ProcessorReference: "AUTH-1"));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(PaymentStatus.Declined));
        await _ledgerClient.DidNotReceive().PostEntryAsync(Arg.Any<PostLedgerEntryRequest>(), Arg.Any<CancellationToken>());
        await _idempotency.Received(1).SaveAsync(Arg.Any<IdempotencyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_ledger_post_failure_marks_the_payment_failed_and_does_not_cache_the_idempotency_key()
    {
        _authorizationClient.AuthorizeAsync(Arg.Any<AuthorizeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AuthorizeResponse(Guid.NewGuid(), Approved: true, DeclineReason: null, ProcessorReference: "AUTH-1"));
        _ledgerClient.PostEntryAsync(Arg.Any<PostLedgerEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PostLedgerEntryResponse(Guid.Empty, Posted: false));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        // Not caching here is deliberate: a transient downstream failure should let a client retry
        // the same Idempotency-Key and actually succeed once Ledger recovers.
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(PaymentStatus.Failed));
        await _idempotency.DidNotReceive().SaveAsync(Arg.Any<IdempotencyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_amount_fails_fast_without_touching_any_downstream_service()
    {
        var command = ValidCommand() with { Amount = -5m };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _payments.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }
}
