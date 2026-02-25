using SignalPulse.Abstractions.Events;
namespace SignalPulse.MarketData.Contracts.Events;

public record QuoteCreated(string Symbol, decimal Price) : IDomainEvent, ISignalREvent<object>
{
    public Guid AggregateId { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string EventType => "quote.created";

    public object Payload => new
    {
        Symbol,
        Price,
    };
}