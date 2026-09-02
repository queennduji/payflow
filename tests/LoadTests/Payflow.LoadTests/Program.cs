using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

// Targets an already-running `docker compose up` stack – load testing exercises a live
// environment on purpose, the same one the README's demo curl sequence and Grafana dashboards
// already point at. It is not started by this tool.
const string GatewayBaseUrl = "http://localhost:8080";
const string KeycloakTokenEndpoint = "http://localhost:8081/realms/payflow/protocol/openid-connect/token";

using var httpClient = new HttpClient { BaseAddress = new Uri(GatewayBaseUrl) };

Console.WriteLine("Fetching a token from Keycloak...");
var token = await GetTokenAsync();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
Console.WriteLine("Token acquired. Starting load test against " + GatewayBaseUrl);

var scenario = Scenario.Create("submit_payment", async context =>
{
    var idempotencyKey = $"load-{context.ScenarioInfo.InstanceId}-{context.InvocationNumber}-{Guid.NewGuid():N}";
    using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
    {
        Content = JsonContent.Create(new { merchantId = "loadtest", amount = 10m, currency = "USD", paymentMethodRef = "tok_visa" }),
    };
    request.Headers.Add("Idempotency-Key", idempotencyKey);

    using var response = await httpClient.SendAsync(request);

    var statusCode = ((int)response.StatusCode).ToString();
    return response.IsSuccessStatusCode ? Response.Ok(statusCode: statusCode) : Response.Fail(statusCode: statusCode);
})
.WithLoadSimulations(
    // A steady climb, not an instant spike – the same shape as the README's own chaos walkthrough
    // (a burst of requests, not a single one), just sustained long enough to see it in Grafana.
    Simulation.RampingInject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder("reports")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
    .Run();

async Task<string> GetTokenAsync()
{
    using var tokenClient = new HttpClient();
    using var response = await tokenClient.PostAsync(KeycloakTokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"] = "payflow-client",
        ["username"] = "demo-merchant",
        ["password"] = "demo-merchant",
    }));

    response.EnsureSuccessStatusCode();
    await using var stream = await response.Content.ReadAsStreamAsync();
    using var document = await JsonDocument.ParseAsync(stream);
    return document.RootElement.GetProperty("access_token").GetString()!;
}
