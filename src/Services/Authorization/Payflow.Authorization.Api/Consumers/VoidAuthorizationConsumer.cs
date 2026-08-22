using MassTransit;
using MediatR;
using Payflow.Authorization.Application.Authorizations;
using Payflow.Shared.Contracts.Messages;

namespace Payflow.Authorization.Api.Consumers;

/// <summary>Handles the saga's compensating transaction — see ADR-0005.</summary>
public sealed class VoidAuthorizationConsumer(ISender sender) : IConsumer<VoidAuthorization>
{
    public async Task Consume(ConsumeContext<VoidAuthorization> context)
    {
        var message = context.Message;
        var result = await sender.Send(new VoidAuthorizationCommand(message.PaymentId));

        if (result.IsFailure)
            throw new InvalidOperationException($"Voiding authorization failed for payment {message.PaymentId}: {result.Error.Message}");

        await context.Publish(new AuthorizationVoided(message.PaymentId, message.AuthorizationId));
    }
}
