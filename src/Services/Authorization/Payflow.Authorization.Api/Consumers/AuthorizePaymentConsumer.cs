using MassTransit;
using MediatR;
using Payflow.Authorization.Application.Authorizations;
using Payflow.Shared.Contracts.Messages;

namespace Payflow.Authorization.Api.Consumers;

public sealed class AuthorizePaymentConsumer(ISender sender) : IConsumer<AuthorizePayment>
{
    public async Task Consume(ConsumeContext<AuthorizePayment> context)
    {
        var message = context.Message;
        var result = await sender.Send(new AuthorizePaymentCommand(message.PaymentId, message.Amount, message.Currency, message.PaymentMethodRef));

        if (result.IsFailure)
            throw new InvalidOperationException($"Authorization failed for payment {message.PaymentId}: {result.Error.Message}");

        if (result.Value.Approved)
            await context.Publish(new PaymentAuthorized(message.PaymentId, result.Value.AuthorizationId, result.Value.ProcessorReference));
        else
            await context.Publish(new PaymentAuthorizationDeclined(message.PaymentId, result.Value.DeclineReason!));
    }
}
