using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record FailureClassification(FailureCategory Category, bool Recoverable, RecoveryStrategy RecommendedStrategy, string Reason);