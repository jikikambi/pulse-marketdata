using SignalPulse.Abstractions.Events;
namespace SignalPulse.MarketData.Contracts.Events;

public record ForexCreated(string FromSymbol, string ToSymbol, decimal Open, decimal High, decimal Low, decimal Close, DateTimeOffset ForexDate) : IDomainEvent, ISignalREvent<object>
{
    public Guid AggregateId { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string EventType => ContractConstants.FxCreatedEventType;

    public object Payload => new
    {
        FromSymbol,
        ToSymbol,
        Open,
        High,
        Low,
        Close,
        ForexDate,
        OccurredAt
    };
}
