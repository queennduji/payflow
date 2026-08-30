using MediatR;
using Payflow.Authorization.Application.Authorizations;
using Payflow.Shared.Api;
using Payflow.Shared.Contracts.Authorization;

namespace Payflow.Authorization.Api.Endpoints;

public static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/authorize", async (AuthorizeRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new AuthorizePaymentCommand(request.PaymentId, request.Amount, request.Currency, request.PaymentMethodRef);
            var result = await sender.Send(command, ct);

            return result.ToHttpResult(auth => Results.Ok(new AuthorizeResponse(
                auth.AuthorizationId, auth.Approved, auth.DeclineReason, auth.ProcessorReference)));
        })
        .WithTags("Authorization")
        .WithName("Authorize")
        .WithSummary("Authorize (mock) a card charge. Idempotent per PaymentId.")
        .Produces<AuthorizeResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization();

        return app;
    }
}
