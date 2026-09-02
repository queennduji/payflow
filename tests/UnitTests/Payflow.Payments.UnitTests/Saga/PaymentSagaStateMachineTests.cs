using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Payflow.Payments.Application.Abstractions;
using Payflow.Payments.Application.Saga;
using Payflow.Payments.Domain;
using Payflow.Shared.Contracts.Messages;
using Payflow.Shared.Kernel;
using PaymentAuthorizedMessage = Payflow.Shared.Contracts.Messages.PaymentAuthorized;

namespace Payflow.Payments.UnitTests.Saga;

/// <summary>
/// Exercises the saga state machine directly against MassTransit's in-memory test harness – no
/// broker, no database. <see cref="IPaymentRepository"/>/<see cref="IUnitOfWork"/> are substituted
/// so these tests verify both the saga's transitions *and* that it drives the Payment aggregate's
/// own status correctly (via the real MarkPayment*Command handlers), without needing Postgres.
/// </summary>
public sealed class PaymentSagaStateMachineTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private IPaymentRepository _payments = null!;

    public async Task InitializeAsync()
    {
        _payments = Substitute.For<IPaymentRepository>();

        var services = new ServiceCollection();
        services.AddSingleton(_payments);
        services.AddSingleton(Substitute.For<IUnitOfWork>());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Payflow.Payments.Application.DependencyInjection).Assembly));

        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<PaymentSagaStateMachine, PaymentSagaState>();
        });

        _provider = services.BuildServiceProvider(validateScopes: true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    private Payment NewTrackedPayment(decimal amount = 100m)
    {
        var payment = Payment.Submit("merchant-1", Money.Create(amount, "USD").Value, "tok_visa", "idem-key").Value;
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        return payment;
    }

    [Fact]
    public async Task Happy_path_reaches_captured_and_notifies()
    {
        var payment = NewTrackedPayment();
        var sagaHarness = _harness.GetSagaStateMachineHarness<PaymentSagaStateMachine, PaymentSagaState>();

        await _harness.Bus.Publish(new ProcessPayment(payment.Id, payment.MerchantId, payment.Amount.Amount, payment.Amount.Currency, payment.PaymentMethodRef));
        (await sagaHarness.Exists(payment.Id, x => x.CheckingFraud)).Should().NotBeNull();
        (await _harness.Published.Any<CheckFraud>()).Should().BeTrue();

        await _harness.Bus.Publish(new FraudCheckPassed(payment.Id));
        (await sagaHarness.Exists(payment.Id, x => x.Authorizing)).Should().NotBeNull();
        (await _harness.Published.Any<AuthorizePayment>()).Should().BeTrue();

        var authorizationId = Guid.NewGuid();
        await _harness.Bus.Publish(new PaymentAuthorizedMessage(payment.Id, authorizationId, "AUTH-REF"));
        (await sagaHarness.Exists(payment.Id, x => x.PostingLedger)).Should().NotBeNull();
        (await _harness.Published.Any<PostLedgerEntry>()).Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Authorized);

        await _harness.Bus.Publish(new LedgerEntryPosted(payment.Id, Guid.NewGuid()));
        // Waiting on the terminal publish is the reliable sync point – it's the last thing the
        // saga's activity chain does, so by the time it's observed, MarkPaymentCapturedCommand has
        // already run.
        (await _harness.Published.Any<SendPaymentNotification>(m => m!.Message!.Status == "Captured")).Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task Fraud_rejection_declines_the_payment_without_ever_contacting_authorization()
    {
        var payment = NewTrackedPayment();
        var sagaHarness = _harness.GetSagaStateMachineHarness<PaymentSagaStateMachine, PaymentSagaState>();

        await _harness.Bus.Publish(new ProcessPayment(payment.Id, payment.MerchantId, payment.Amount.Amount, payment.Amount.Currency, payment.PaymentMethodRef));
        (await sagaHarness.Exists(payment.Id, x => x.CheckingFraud)).Should().NotBeNull();

        await _harness.Bus.Publish(new FraudCheckFailed(payment.Id, "velocity_limit_exceeded"));

        (await _harness.Published.Any<SendPaymentNotification>(m => m!.Message!.Status == "Declined")).Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Declined);
        (await _harness.Published.Any<AuthorizePayment>()).Should().BeFalse();
    }

    [Fact]
    public async Task Ledger_failure_after_authorization_voids_the_authorization_and_fails_the_payment()
    {
        var payment = NewTrackedPayment();
        var sagaHarness = _harness.GetSagaStateMachineHarness<PaymentSagaStateMachine, PaymentSagaState>();
        var authorizationId = Guid.NewGuid();

        await _harness.Bus.Publish(new ProcessPayment(payment.Id, payment.MerchantId, payment.Amount.Amount, payment.Amount.Currency, payment.PaymentMethodRef));
        await _harness.Bus.Publish(new FraudCheckPassed(payment.Id));
        await _harness.Bus.Publish(new PaymentAuthorizedMessage(payment.Id, authorizationId, "AUTH-REF"));
        (await sagaHarness.Exists(payment.Id, x => x.PostingLedger)).Should().NotBeNull();

        await _harness.Bus.Publish(new LedgerPostFailed(payment.Id, "insufficient_liquidity"));

        // The compensating transaction: authorization must be voided before the payment can fail.
        (await sagaHarness.Exists(payment.Id, x => x.VoidingAuthorization)).Should().NotBeNull();
        (await _harness.Published.Any<VoidAuthorization>(m => m!.Message!.AuthorizationId == authorizationId)).Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Authorized); // not yet failed – still waiting on the void to confirm

        await _harness.Bus.Publish(new AuthorizationVoided(payment.Id, authorizationId));

        (await _harness.Published.Any<SendPaymentNotification>(m => m!.Message!.Status == "Failed")).Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("insufficient_liquidity");
    }
}
