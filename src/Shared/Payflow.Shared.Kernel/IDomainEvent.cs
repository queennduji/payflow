namespace Payflow.Shared.Kernel;

/// <summary>
/// Marker for something that happened inside an aggregate that other parts of the same bounded
/// context (and, via an outbox, other services) may care about. Kept free of any messaging
/// framework dependency so the Domain layer stays framework-agnostic; the Application layer is
/// responsible for translating these into MediatR notifications / outbox messages.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}
