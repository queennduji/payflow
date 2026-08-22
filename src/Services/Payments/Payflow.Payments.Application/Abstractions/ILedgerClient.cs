using Payflow.Shared.Contracts.Ledger;

namespace Payflow.Payments.Application.Abstractions;

/// <summary>Outbound port to the Ledger service for posting the debit/credit pair of a captured payment.</summary>
public interface ILedgerClient
{
    Task<PostLedgerEntryResponse> PostEntryAsync(PostLedgerEntryRequest request, CancellationToken cancellationToken);
}
