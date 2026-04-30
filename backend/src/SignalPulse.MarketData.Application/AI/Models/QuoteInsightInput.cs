namespace SignalPulse.MarketData.Application.AI.Models;

public record QuoteInsightInput(string Symbol, decimal Price, decimal ChangePercent, long Volume, Guid CorrelationId);
