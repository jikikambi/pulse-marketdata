namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class MarketAgentState
{
    public Guid CorrelationId { get; init; }
    public string Symbol { get; init; } = default!;
    public string? PlanJson { get; set; }
    public string? ToolContextJson { get; set; }
    public string? FinalResultJson { get; set; }
    public bool ToolUsed { get; set; }
    public bool Completed { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<AgentStep> Steps { get; set; } = [];
    public List<StageExecutionResult> StageResults { get; set; } = [];
    public ConfidenceScoreResult? Confidence { get; set; }
    public FinalDecisionResult? FinalDecision { get; set; }
}