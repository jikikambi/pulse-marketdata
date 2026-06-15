using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record StageFailureAction(
    RecoveryStrategy Strategy,
    string? Reason = null,
    MarketAgentStage? FallbackStage = null,
    MarketAgentStage? AlternateStage = null);