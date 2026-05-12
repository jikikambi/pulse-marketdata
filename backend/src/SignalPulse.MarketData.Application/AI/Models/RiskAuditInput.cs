using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record RiskAuditInput(string Symbol, decimal ChangePercent, VolatilityType Volatility, SentimentType Sentiment);
