using System.Net.Http.Json;
using Payflow.Payments.Application.Abstractions;
using Payflow.Shared.Contracts.Authorization;

namespace Payflow.Payments.Infrastructure.Clients;

public sealed class HttpAuthorizationClient(HttpClient httpClient) : IAuthorizationClient
{
    public async Task<AuthorizeResponse> AuthorizeAsync(AuthorizeRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/authorize", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthorizeResponse>(cancellationToken))!;
    }
}
