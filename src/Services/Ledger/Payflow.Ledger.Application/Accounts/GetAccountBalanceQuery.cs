using MediatR;
using Payflow.Ledger.Application.Abstractions;
using Payflow.Ledger.Domain;
using Payflow.Shared.Kernel;

namespace Payflow.Ledger.Application.Accounts;

public sealed record GetAccountBalanceQuery(string AccountId) : IRequest<Result<AccountBalanceResponse>>;

public sealed record AccountBalanceResponse(string AccountId, decimal Balance, string Currency);

public sealed class GetAccountBalanceQueryHandler(IAccountRepository accounts)
    : IRequestHandler<GetAccountBalanceQuery, Result<AccountBalanceResponse>>
{
    public async Task<Result<AccountBalanceResponse>> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
            return Result.Failure<AccountBalanceResponse>(Error.NotFound("Account.NotFound", $"No account with id '{request.AccountId}'."));

        var lines = await accounts.GetLinesForAccountAsync(request.AccountId, cancellationToken);
        var balance = AccountBalanceCalculator.Compute(account.Type, lines);

        return Result.Success(new AccountBalanceResponse(account.Id, balance, account.Currency));
    }
}
