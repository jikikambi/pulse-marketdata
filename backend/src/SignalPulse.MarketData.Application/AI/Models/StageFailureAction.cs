using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed record StageFailureAction(
    bool ContinueWorkflow,
    bool RetryStage,
    bool UseFallback,
    bool TerminateWorkflow,
    MarketAgentStage? FallbackStage = null,
    string? Reason = null);