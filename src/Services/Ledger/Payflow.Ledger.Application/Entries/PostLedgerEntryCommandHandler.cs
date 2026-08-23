using MediatR;
using Payflow.Ledger.Application.Abstractions;
using Payflow.Ledger.Domain;
using Payflow.Shared.Kernel;

namespace Payflow.Ledger.Application.Entries;

/// <summary>
/// Posts a balanced debit/credit pair for a captured payment. Idempotent per PaymentId: Payments
/// calls this over plain HTTP with at-least-once semantics (Phase 1), so a retried call must be a
/// no-op that returns the original result rather than double-posting money.
/// </summary>
public sealed class PostLedgerEntryCommandHandler(
    ILedgerEntryGroupRepository groups,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PostLedgerEntryCommand, Result<LedgerEntryGroupResponse>>
{
    public async Task<Result<LedgerEntryGroupResponse>> Handle(PostLedgerEntryCommand request, CancellationToken cancellationToken)
    {
        var existing = await groups.GetByPaymentIdAsync(request.PaymentId, cancellationToken);
        if (existing is not null)
            return Result.Success(new LedgerEntryGroupResponse(existing.Id, Posted: true));

        // EF Core tracks owned types (Money, owned by each LedgerLine) by reference identity, so
        // the debit and credit lines each need their own Money instance — sharing one object
        // across two sibling owned entities silently corrupts change tracking (one line's amount
        // ends up NULL in the database instead of raising an error).
        var debitAmountResult = Money.Create(request.Amount, request.Currency);
        var creditAmountResult = Money.Create(request.Amount, request.Currency);
        if (debitAmountResult.IsFailure)
            return Result.Failure<LedgerEntryGroupResponse>(debitAmountResult.Error);
        if (creditAmountResult.IsFailure)
            return Result.Failure<LedgerEntryGroupResponse>(creditAmountResult.Error);

        await accounts.GetOrOpenAsync(request.DebitAccountId, request.Currency, cancellationToken);
        await accounts.GetOrOpenAsync(request.CreditAccountId, request.Currency, cancellationToken);

        var lines = new[]
        {
            LedgerLine.Debit(request.DebitAccountId, debitAmountResult.Value),
            LedgerLine.Credit(request.CreditAccountId, creditAmountResult.Value)
        };

        var groupResult = LedgerEntryGroup.Post(request.PaymentId, lines);
        if (groupResult.IsFailure)
            return Result.Failure<LedgerEntryGroupResponse>(groupResult.Error);

        await groups.AddAsync(groupResult.Value, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (LedgerPostConflictException)
        {
            var winner = await groups.GetByPaymentIdAsync(request.PaymentId, cancellationToken)
                ?? throw new InvalidOperationException("Lost a ledger-post race but the winning group is missing.");
            return Result.Success(new LedgerEntryGroupResponse(winner.Id, Posted: true));
        }

        return Result.Success(new LedgerEntryGroupResponse(groupResult.Value.Id, Posted: true));
    }
}
