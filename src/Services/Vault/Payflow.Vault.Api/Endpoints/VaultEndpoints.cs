using MediatR;
using Payflow.Shared.Api;
using Payflow.Vault.Application.Tokenization;

namespace Payflow.Vault.Api.Endpoints;

public static class VaultEndpoints
{
    public static IEndpointRouteBuilder MapVaultEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tokenize", async (TokenizeCardRequest body, ISender sender, CancellationToken ct) =>
        {
            var command = new TokenizeCardCommand(body.CardNumber, body.ExpiryMonth, body.ExpiryYear);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithTags("Vault")
        .WithName("TokenizeCard")
        .WithSummary("Exchange a raw card number for an opaque token. The card number is never stored or logged.")
        .Produces<TokenizeCardResult>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization();

        return app;
    }
}

public sealed record TokenizeCardRequest(string CardNumber, int ExpiryMonth, int ExpiryYear);
