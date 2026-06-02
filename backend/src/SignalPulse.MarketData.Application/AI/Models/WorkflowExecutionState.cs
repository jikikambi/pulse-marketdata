using SignalPulse.MarketData.Application.AI.Models.Enums;
using System.Collections.Concurrent;

namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class WorkflowExecutionState
{
    public ConcurrentDictionary<MarketAgentStage, StageExecutionStatus> Statuses { get; } = new();

    public bool IsCompleted(MarketAgentStage stage) => Statuses.TryGetValue(stage, out var status) && status == StageExecutionStatus.Completed;
    public bool IsFailed(MarketAgentStage stage) => Statuses.TryGetValue(stage, out var status) && status == StageExecutionStatus.Failed;
}