using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Payflow.IntegrationTests;

/// <summary>Its own cluster, not <see cref="PaymentFlowClusterFixture"/>'s – Authorization needs a
/// cranked-up fault rate here, which the happy-path cluster deliberately keeps at zero.</summary>
public sealed class ChaosClusterFixture(PayflowInfrastructureFixture infrastructure) : IAsyncLifetime
{
    public PayflowServiceCluster Cluster { get; private set; } = null!;

    public async Task InitializeAsync() => Cluster = await PayflowServiceCluster.StartAsync(infrastructure, authorizationFaultRate: 1.0);

    public async Task DisposeAsync() => await Cluster.DisposeAsync();
}

/// <summary>
/// Turns Phase 3's fault-injection knob (<c>Chaos:CardNetworkFaultRate</c>) into an assertion
/// instead of something only ever watched by eye in Grafana. With the mock card network failing
/// outright on every call, every one of several sequential payments should still resolve – as a
/// graceful decline, never a fault – because Authorization's resilience pipeline (retry, then
/// circuit breaker) degrades to `processor_unavailable` rather than letting the fault escape.
/// </summary>
/// <remarks>
/// Sequential on purpose, not concurrent: firing this same burst as truly-simultaneous requests
/// (<c>Task.WhenAll</c>, no stagger) reproducibly surfaces a separate, real bug – MassTransit's EF
/// saga repository can fault a brand-new saga's own <c>Initially()</c> transition with "entity ...
/// already being tracked" when enough *different* payments' saga rows get created on Postgres in
/// the same instant, under EF Core's <c>EnableRetryOnFailure</c> retrying an attempt whose change
/// tracker still holds state from the try that lost the SERIALIZABLE conflict. That's a genuine
/// saga/EF-retry concurrency issue worth its own fix, not something this class should paper over
/// by weakening what it actually asserts – tracked as follow-up work.
/// </remarks>
[Collection(PayflowCollection.Name)]
public sealed class ResilienceChaosTests(ChaosClusterFixture clusterFixture, PayflowInfrastructureFixture infrastructure)
    : IClassFixture<ChaosClusterFixture>
{
    [Fact]
    public async Task A_100_percent_fault_rate_degrades_every_payment_to_a_graceful_decline()
    {
        var token = await KeycloakTokenClient.GetDemoMerchantTokenAsync(infrastructure);

        for (var i = 0; i < 5; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = JsonContent.Create(new { merchantId = "acme", amount = 10m, currency = "USD", paymentMethodRef = "tok_visa" }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Idempotency-Key", $"it-chaos-{i}-{Guid.NewGuid()}");

            var response = await clusterFixture.Cluster.PaymentsClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            response.IsSuccessStatusCode.Should().BeTrue(body);
            var payment = JsonSerializer.Deserialize<PaymentResult>(body, JsonSerializerOptions.Web);
            payment!.Status.Should().Be("Declined");
            payment.FailureReason.Should().Be("processor_unavailable");
        }
    }

    private sealed record PaymentResult(Guid PaymentId, string Status, string? FailureReason);
}
