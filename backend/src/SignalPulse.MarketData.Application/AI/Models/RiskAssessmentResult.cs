using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record RiskAssessmentResult(bool IsRisky, string Reason, RiskLevel Level);
