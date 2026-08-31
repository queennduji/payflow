using System.Text.Json;

namespace Payflow.IntegrationTests;

/// <summary>Fetches a real access token the same way the README's demo does — Resource Owner
/// Password Credentials against the realm every service in the cluster validates against.</summary>
public static class KeycloakTokenClient
{
    public static async Task<string> GetDemoMerchantTokenAsync(PayflowInfrastructureFixture fixture)
    {
        using var client = new HttpClient();
        using var response = await client.PostAsync(fixture.KeycloakTokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
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
}
