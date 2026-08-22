using MediatR;
using Payflow.Shared.Kernel;

namespace Payflow.Ledger.Application.Entries;

public sealed record PostLedgerEntryCommand(
    Guid PaymentId,
    string DebitAccountId,
    string CreditAccountId,
    decimal Amount,
    string Currency) : IRequest<Result<LedgerEntryGroupResponse>>;

public sealed record LedgerEntryGroupResponse(Guid EntryGroupId, bool Posted);
