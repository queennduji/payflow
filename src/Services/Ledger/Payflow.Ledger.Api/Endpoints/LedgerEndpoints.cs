using MediatR;
using Payflow.Ledger.Application.Accounts;
using Payflow.Ledger.Application.Entries;
using Payflow.Shared.Api;

namespace Payflow.Ledger.Api.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/entries", async (PostLedgerEntryCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithTags("Ledger")
        .WithName("PostLedgerEntry")
        .WithSummary("Post a balanced debit/credit pair for a captured payment. Idempotent per PaymentId.")
        .Produces<LedgerEntryGroupResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGet("/accounts/{accountId}/balance", async (string accountId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAccountBalanceQuery(accountId), ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithTags("Ledger")
        .WithName("GetAccountBalance")
        .WithSummary("Get an account's current balance, derived from its posted ledger lines.")
        .Produces<AccountBalanceResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
