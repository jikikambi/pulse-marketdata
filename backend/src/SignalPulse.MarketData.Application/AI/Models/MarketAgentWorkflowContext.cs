using SignalPulse.MarketData.Infrastructure.Elastic;
using SignalPulse.MarketData.Infrastructure.Policies.Contracts;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class MarketAgentWorkflowContext : IPolicyEventEmitter
{
    public required QuoteInsightInput Input { get; init; }
    public required IWorkflowEventSink EventSink { get; init; }
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
    public string CorrelationId => Input.CorrelationId.ToString();
    public string? PlanRaw { get; set; }
    public CancellationTokenSource? WorkflowCancellationSource { get; set; }

    public void Terminate(AIInsightResult result)
    {
        FinalResult = result;

        _ = EmitAsync("workflow", "workflow_terminated", result.Rationale, new { result });
    }

    public void AddStep(string name, string input, string output)
    {
        State.Steps.Add(new AgentStep(name, input, output, DateTimeOffset.UtcNow));

        State.UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Workflow telemetry
    public Task EmitAsync(string stage, string type, string message, object? metadata = null, CancellationToken ct = default) =>
        EventSink.WriteAsync(new WorkflowEvent(Input.CorrelationId, stage, type, message, DateTimeOffset.UtcNow, metadata), ct);
}