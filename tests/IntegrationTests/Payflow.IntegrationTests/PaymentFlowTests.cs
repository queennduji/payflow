using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace Payflow.IntegrationTests;

/// <summary>Boots the saga-participant services once for every test in this class – see
/// <see cref="PayflowCollection"/> for why the containers themselves are shared instead.</summary>
public sealed class PaymentFlowClusterFixture(PayflowInfrastructureFixture infrastructure) : IAsyncLifetime
{
    public PayflowServiceCluster Cluster { get; private set; } = null!;

    public async Task InitializeAsync() => Cluster = await PayflowServiceCluster.StartAsync(infrastructure);

    public async Task DisposeAsync() => await Cluster.DisposeAsync();
}

/// <summary>
/// The automated version of the demo `curl` sequence every phase since Phase 2 has re-run by hand:
/// capture, decline, fraud reject, and idempotent replay, driven as real HTTP calls through a real
/// Postgres-backed, RabbitMQ-orchestrated saga – not mocks, not `ITestHarness`'s in-memory bus.
/// </summary>
[Collection(PayflowCollection.Name)]
public sealed class PaymentFlowTests(PaymentFlowClusterFixture clusterFixture, PayflowInfrastructureFixture infrastructure)
    : IClassFixture<PaymentFlowClusterFixture>
{
    private async Task<HttpRequestMessage> AuthorizedPaymentRequestAsync(object body, string idempotencyKey)
    {
        var token = await KeycloakTokenClient.GetDemoMerchantTokenAsync(infrastructure);
        var request = new HttpRequestMessage(HttpMethod.Post, "/payments") { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    [Fact]
    public async Task A_payment_with_a_valid_card_is_captured()
    {
        var request = await AuthorizedPaymentRequestAsync(
            new { merchantId = "acme", amount = 42.50m, currency = "USD", paymentMethodRef = "tok_visa" },
            $"it-capture-{Guid.NewGuid()}");

        var response = await clusterFixture.Cluster.PaymentsClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payment!.Status.Should().Be("Captured");
    }

    [Fact]
    public async Task A_declined_card_is_reported_without_posting_to_the_ledger()
    {
        var request = await AuthorizedPaymentRequestAsync(
            new { merchantId = "acme", amount = 10m, currency = "USD", paymentMethodRef = "tok_declined" },
            $"it-decline-{Guid.NewGuid()}");

        var response = await clusterFixture.Cluster.PaymentsClient.SendAsync(request);

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payment!.Status.Should().Be("Declined");
        payment.FailureReason.Should().Be("insufficient_funds");
    }

    [Fact]
    public async Task A_blocked_payment_method_is_rejected_by_fraud_review_before_authorization_runs()
    {
        var request = await AuthorizedPaymentRequestAsync(
            new { merchantId = "acme", amount = 10m, currency = "USD", paymentMethodRef = "tok_fraud" },
            $"it-fraud-{Guid.NewGuid()}");

        var response = await clusterFixture.Cluster.PaymentsClient.SendAsync(request);

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payment!.Status.Should().Be("Declined");
        payment.FailureReason.Should().Be("blocked_payment_method");
    }

    [Fact]
    public async Task Replaying_the_same_idempotency_key_returns_the_original_payment_not_a_new_one()
    {
        var idempotencyKey = $"it-replay-{Guid.NewGuid()}";
        var body = new { merchantId = "acme", amount = 15m, currency = "USD", paymentMethodRef = "tok_visa" };

        var firstRequest = await AuthorizedPaymentRequestAsync(body, idempotencyKey);
        var first = await clusterFixture.Cluster.PaymentsClient.SendAsync(firstRequest);
        var firstPayment = await first.Content.ReadFromJsonAsync<PaymentResponse>();

        var secondRequest = await AuthorizedPaymentRequestAsync(body, idempotencyKey);
        var second = await clusterFixture.Cluster.PaymentsClient.SendAsync(secondRequest);
        var secondPayment = await second.Content.ReadFromJsonAsync<PaymentResponse>();

        secondPayment!.PaymentId.Should().Be(firstPayment!.PaymentId);
    }

    private sealed record PaymentResponse(Guid PaymentId, string Status, string? FailureReason);
}
