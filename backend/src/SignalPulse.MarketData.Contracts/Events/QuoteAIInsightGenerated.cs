using SignalPulse.Abstractions.Events;

namespace SignalPulse.MarketData.Contracts.Events;

public record QuoteAIInsightGenerated(
    Guid Id,
    string Symbol,
    decimal Price,
    string Sentiment,
    string Direction,
    string Volatility,
    string Rationale,
    DateTimeOffset ObservedAt) : ISignalREvent<object>
{
    public string EventType => ContractConstants.QuoteAIInsightEventType;

    public object Payload => new
    {
        Id,
        Symbol,
        Price,
        Sentiment,
        Direction,
        Volatility,
        Rationale,
        ObservedAt
    };
}