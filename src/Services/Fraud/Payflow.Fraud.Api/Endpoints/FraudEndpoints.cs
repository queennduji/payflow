using MediatR;
using Payflow.Fraud.Application.FraudChecks;
using Payflow.Shared.Api;

namespace Payflow.Fraud.Api.Endpoints;

public static class FraudEndpoints
{
    public static IEndpointRouteBuilder MapFraudEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/fraud-checks/{paymentId:guid}", async (Guid paymentId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetFraudCheckByPaymentIdQuery(paymentId), ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithTags("Fraud")
        .WithName("GetFraudCheck")
        .WithSummary("Inspect the fraud decision recorded for a payment (demo/audit visibility).")
        .Produces<FraudCheckResult>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
