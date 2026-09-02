using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace Payflow.IntegrationTests;

/// <summary>
/// Closes the gap Phase 6 explicitly deferred: real JWT validation needs an actual token from a
/// running Keycloak, which a unit test can't produce. Reuses <see cref="PaymentFlowClusterFixture"/>
/// – this is the exact same cluster, just called with and without a real bearer token.
/// </summary>
[Collection(PayflowCollection.Name)]
public sealed class AuthenticationTests(PaymentFlowClusterFixture clusterFixture, PayflowInfrastructureFixture infrastructure)
    : IClassFixture<PaymentFlowClusterFixture>
{
    private static readonly object PaymentBody = new { merchantId = "acme", amount = 10m, currency = "USD", paymentMethodRef = "tok_visa" };

    [Fact]
    public async Task A_request_with_no_token_is_rejected()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/payments") { Content = JsonContent.Create(PaymentBody) };
        request.Headers.Add("Idempotency-Key", $"it-noauth-{Guid.NewGuid()}");

        var response = await clusterFixture.Cluster.PaymentsClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_request_with_a_real_keycloak_token_is_accepted()
    {
        var token = await KeycloakTokenClient.GetDemoMerchantTokenAsync(infrastructure);
        var request = new HttpRequestMessage(HttpMethod.Post, "/payments") { Content = JsonContent.Create(PaymentBody) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Idempotency-Key", $"it-auth-{Guid.NewGuid()}");

        var response = await clusterFixture.Cluster.PaymentsClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_token_with_a_tampered_signature_is_rejected()
    {
        // A genuine token, with its signature corrupted after the fact – proves validation
        // actually verifies against Keycloak's JWKS rather than trusting the token's own claims.
        var token = await KeycloakTokenClient.GetDemoMerchantTokenAsync(infrastructure);
        var tamperedToken = token[..^4] + (token[^4..] == "abcd" ? "efgh" : "abcd");

        var request = new HttpRequestMessage(HttpMethod.Post, "/payments") { Content = JsonContent.Create(PaymentBody) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);
        request.Headers.Add("Idempotency-Key", $"it-tampered-{Guid.NewGuid()}");

        var response = await clusterFixture.Cluster.PaymentsClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
