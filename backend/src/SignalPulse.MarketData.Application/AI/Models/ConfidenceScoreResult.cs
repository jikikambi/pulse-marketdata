using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record ConfidenceScoreResult(double Score, ConfidenceLevel Level, string Reason);