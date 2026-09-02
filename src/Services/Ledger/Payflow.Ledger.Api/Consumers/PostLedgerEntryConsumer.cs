using MassTransit;
using MediatR;
using Payflow.Ledger.Application.Entries;
using Payflow.Shared.Contracts.Messages;

namespace Payflow.Ledger.Api.Consumers;

public sealed class PostLedgerEntryConsumer(ISender sender) : IConsumer<PostLedgerEntry>
{
    public async Task Consume(ConsumeContext<PostLedgerEntry> context)
    {
        var message = context.Message;
        var result = await sender.Send(new PostLedgerEntryCommand(
            message.PaymentId, message.DebitAccountId, message.CreditAccountId, message.Amount, message.Currency));

        if (result.IsFailure)
        {
            // A validation failure (e.g. an unbalanced posting) is not transient – retrying it would
            // fail identically, so report it to the saga as a definitive LedgerPostFailed rather than
            // letting MassTransit's consumer retry/redelivery keep hammering it.
            await context.Publish(new LedgerPostFailed(message.PaymentId, result.Error.Message));
            return;
        }

        await context.Publish(new LedgerEntryPosted(message.PaymentId, result.Value.EntryGroupId));
    }
}
