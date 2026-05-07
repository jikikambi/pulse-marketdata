using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record AIInsightResult(SentimentType Sentiment, DirectionType Direction, VolatilityType Volatility, string Rationale);