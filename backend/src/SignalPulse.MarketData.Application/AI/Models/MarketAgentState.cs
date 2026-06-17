using SignalPulse.MarketData.Application.AI.Models.Enums;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class MarketAgentState
{
    private readonly Dictionary<MarketAgentStage, int> _failures = [];

    public Guid CorrelationId { get; init; }
    public string Symbol { get; init; } = default!;
    public string? PlanJson { get; set; }
    public string? ToolContextJson { get; set; }
    public string? FinalResultJson { get; set; }
    public bool ToolUsed { get; set; }
    public bool Completed { get; set; }
    public bool IsDegradedMode { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<AgentStep> Steps { get; set; } = [];
    public List<StageExecutionResult> StageResults { get; set; } = [];
    public ConfidenceScoreResult? Confidence { get; set; }
    public FinalDecisionResult? FinalDecision { get; set; }
    public Dictionary<MarketAgentStage, int> FailureCounts { get; init; } = [];
    public Dictionary<MarketAgentStage, int> RecoveryCounts { get; init; } = [];
    public bool IsRecoveryMode { get; set; }
    public RecoverySummary? RecoverySummary { get; set; }
    public List<RecoveryEvent> Recoveries { get; set; } = [];
    public int GetFailureCount(MarketAgentStage stage) => _failures.TryGetValue(stage, out var count) ? count : 0;
    public IReadOnlyDictionary<MarketAgentStage, int> Failures => _failures;
    public Dictionary<MarketAgentStage, string> AlternateAgentsUsed { get; } = [];
    public void IncrementRecovery(MarketAgentStage stage)
    {
        RecoveryCounts.TryAdd(stage, 0);
        RecoveryCounts[stage]++;
    }

    public void IncrementFailure(MarketAgentStage stage)
    {
        _failures[stage] = GetFailureCount(stage) + 1;
    }
}