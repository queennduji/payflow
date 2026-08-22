using MassTransit;
using MediatR;
using Payflow.Fraud.Application.FraudChecks;
using Payflow.Shared.Contracts.Messages;

namespace Payflow.Fraud.Api.Consumers;

public sealed class CheckFraudConsumer(ISender sender) : IConsumer<CheckFraud>
{
    public async Task Consume(ConsumeContext<CheckFraud> context)
    {
        var message = context.Message;
        var result = await sender.Send(new EvaluateFraudCommand(
            message.PaymentId, message.MerchantId, message.Amount, message.Currency, message.PaymentMethodRef));

        if (result.IsFailure)
            throw new InvalidOperationException($"Fraud evaluation failed for payment {message.PaymentId}: {result.Error.Message}");

        if (result.Value.Approved)
            await context.Publish(new FraudCheckPassed(message.PaymentId));
        else
            await context.Publish(new FraudCheckFailed(message.PaymentId, result.Value.Reason!));
    }
}
