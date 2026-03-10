using SignalPulse.Abstractions.Events;

namespace SignalPulse.MarketData.Contracts.Events;

public record QuoteUpdated(string Symbol, decimal Price, decimal ChangePercent) : IDomainEvent, ISignalREvent<object>
{
    public Guid AggregateId { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string EventType => ContractConstants.QuoteUpdatedEventType;

    public object Payload => new 
    {
        Symbol,
        Price,
        ChangePercent,
        OccurredAt
    };
}
