using MediatR;
using Payflow.Payments.Application.Payments;
using Payflow.Shared.Api;

namespace Payflow.Payments.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments").WithTags("Payments").RequireAuthorization();

        group.MapPost("/", async (SubmitPaymentRequest body, HttpRequest request, ISender sender, CancellationToken ct) =>
        {
            if (!request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) || string.IsNullOrWhiteSpace(idempotencyKey))
                return Results.Problem(title: "Payment.IdempotencyKeyMissing", detail: "The Idempotency-Key header is required.", statusCode: StatusCodes.Status400BadRequest);

            var command = new SubmitPaymentCommand(body.MerchantId, body.Amount, body.Currency, body.PaymentMethodRef, idempotencyKey!);
            var result = await sender.Send(command, ct);

            // Completed: the saga finished within the wait window, respond exactly as Phase 1 did.
            // Pending: still in flight (or the caller raced an existing attempt) – report 202 with
            // a Location the client can poll. See ADR-0006.
            return result.ToHttpResult(outcome => outcome switch
            {
                SubmitPaymentCompleted c => Results.Created($"/payments/{c.Payment.PaymentId}", c.Payment),
                SubmitPaymentPending p => Results.Accepted($"/payments/{p.PaymentId}"),
                _ => throw new InvalidOperationException($"Unhandled {nameof(SubmitPaymentOutcome)} type {outcome.GetType().Name}.")
            });
        })
        .WithName("SubmitPayment")
        .WithSummary("Submit a payment for authorization, capture, and ledger posting.")
        .Produces<PaymentResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPaymentByIdQuery(id), ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithName("GetPayment")
        .WithSummary("Fetch a payment by id.")
        .Produces<PaymentResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

public sealed record SubmitPaymentRequest(string MerchantId, decimal Amount, string Currency, string PaymentMethodRef);
