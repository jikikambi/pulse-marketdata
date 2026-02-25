namespace SignalPulse.MarketData.Contracts.Events;

public record AIInsightGenerated( Guid QuoteId, string Symbol, decimal Price, string Sentiment, string Direction, string Volatility, string Rationale, DateTimeOffset Timestamp);