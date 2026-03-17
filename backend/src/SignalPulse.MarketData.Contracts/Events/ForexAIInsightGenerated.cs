using SignalPulse.Abstractions.Events;

namespace SignalPulse.MarketData.Contracts.Events;

public record ForexAIInsightGenerated(
    Guid Id,
    string FromSymbol,
    string ToSymbol,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    DateTimeOffset ForexDate,
    string Sentiment,
    string Direction,
    string Volatility,
    string Rationale,
    DateTimeOffset ObservedAt) : ISignalREvent<object>
{
    public string EventType => ContractConstants.ForexAIInsightEventType;

    public object Payload => new
    {
        Id,
        FromSymbol,
        ToSymbol,
        Open,
        High,
        Low,
        Close,
        ForexDate,
        Sentiment,
        Direction,
        Volatility,
        Rationale,
        ObservedAt
    };
}