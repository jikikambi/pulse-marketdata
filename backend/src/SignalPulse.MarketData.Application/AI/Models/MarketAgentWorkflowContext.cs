using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Diagnostics;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class MarketAgentWorkflowContext
{
    public required QuoteInsightInput Input { get; init; }
    public PlannerResult? Plan { get; set; }
    public string? ToolContextJson { get; set; }
    public AIInsightResult? Insight { get; set; }
    public RiskAssessmentResult? Risk { get; set; }
    public MarketAgentState State { get; set; } = new();
    public List<StageExecutionResult> StageResults { get; } = [];
    public ValidationResult? Validation { get; set; }
    public ConfidenceScoreResult? Confidence { get; set; }
    public FinalDecisionResult? FinalDecision { get; set; }
    public AIInsightResult? FinalResult { get; private set; }
    public bool IsTerminated => FinalResult is not null;
    public MarketAgentStage CurrentStage { get; set; }
    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();
    public string CorrelationId => Input.CorrelationId.ToString();
    public string? PlanRaw { get; set; }
    public void Terminate(AIInsightResult result)
    {
        FinalResult = result;
    }

    public void AddStep(string name, string input, string output)
    {
        State.Steps.Add(new AgentStep(name, input, output, DateTimeOffset.UtcNow));

        State.UpdatedAt = DateTimeOffset.UtcNow;
    }
}