using System.Net.Http.Json;
using Payflow.Payments.Application.Abstractions;
using Payflow.Shared.Contracts.Ledger;

namespace Payflow.Payments.Infrastructure.Clients;

public sealed class HttpLedgerClient(HttpClient httpClient) : ILedgerClient
{
    public async Task<PostLedgerEntryResponse> PostEntryAsync(PostLedgerEntryRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/entries", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PostLedgerEntryResponse>(cancellationToken))!;
    }
}
