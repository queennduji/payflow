using MediatR;
using Payflow.Notifications.Application.Notifications;
using Payflow.Shared.Api;

namespace Payflow.Notifications.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/notifications", async (string merchantId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListNotificationsByMerchantQuery(merchantId), ct);
            return result.ToHttpResult(Results.Ok);
        })
        .WithTags("Notifications")
        .WithName("ListNotifications")
        .WithSummary("List simulated webhook deliveries for a merchant (demo/audit visibility).")
        .Produces<IReadOnlyList<NotificationResult>>();

        return app;
    }
}
