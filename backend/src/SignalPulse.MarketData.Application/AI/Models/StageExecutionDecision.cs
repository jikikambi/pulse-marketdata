using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record StageExecutionDecision(
    bool Execute = true,
    bool Skip = false,
    bool UseFallback = false,
    bool UseRecovery = false,
    bool UseDegradedMode = false,
    MarketAgentStage? FallbackStage = null,
    MarketAgentStage? RecoveryStage = null,
    string? Reason = null);
