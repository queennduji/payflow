using MassTransit;
using MediatR;
using Payflow.Notifications.Application.Notifications;
using Payflow.Shared.Contracts.Messages;

namespace Payflow.Notifications.Api.Consumers;

public sealed class SendPaymentNotificationConsumer(ISender sender) : IConsumer<SendPaymentNotification>
{
    public async Task Consume(ConsumeContext<SendPaymentNotification> context)
    {
        var message = context.Message;
        var result = await sender.Send(new SendNotificationCommand(message.PaymentId, message.MerchantId, message.Status));

        if (result.IsFailure)
            throw new InvalidOperationException($"Notification failed for payment {message.PaymentId}: {result.Error.Message}");

        await context.Publish(new PaymentNotificationSent(message.PaymentId, message.MerchantId));
    }
}
