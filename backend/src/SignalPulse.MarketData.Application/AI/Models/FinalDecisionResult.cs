using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record FinalDecisionResult(DecisionOutcome Outcome, string Reason);