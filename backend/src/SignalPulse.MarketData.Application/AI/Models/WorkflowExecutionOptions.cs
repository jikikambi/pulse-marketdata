namespace SignalPulse.MarketData.Application.AI.Models;

public sealed class WorkflowExecutionOptions
{
    public int MaxConcurrentStages { get; init; } = 4;
    public int MaxConcurrentWorkflows { get; init; } = 50;
}