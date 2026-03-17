namespace SignalPulse.MarketData.Application.AI;

public record QuoteInsightInput(string Symbol, decimal Price, decimal ChangePercent, long Volume);
