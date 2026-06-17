using System.Diagnostics.Metrics;

namespace SignalPulse.MarketData.Application.AI.Services;

public static class ObservabilityMetrics
{
    public static readonly Meter Meter = new("SignalPulse.MarketAgent");
    // Handler metrics
    public static readonly Counter<long> QuotesProcessed = Meter.CreateCounter<long>("quotes_processed");
    public static readonly Counter<long> ForexProcessed = Meter.CreateCounter<long>("forex_processed");
    public static readonly Counter<long> HandlerFailures = Meter.CreateCounter<long>("handler_failures");
    // Workflow metrics
    public static readonly Counter<long> WorkflowStarted = Meter.CreateCounter<long>("marketagent.workflow.started");
    public static readonly Counter<long> WorkflowCompleted = Meter.CreateCounter<long>("marketagent.workflow.completed");
    public static readonly Counter<long> WorkflowFailed = Meter.CreateCounter<long>("marketagent.workflow.failed");
    public static readonly Counter<long> RecoveryApplied = Meter.CreateCounter<long>("marketagent.workflow.recoveries");
    public static readonly Histogram<double> StageDuration = Meter.CreateHistogram<double>("marketagent.stage.duration", unit: "ms");
    public static readonly Histogram<double> WorkflowDuration = Meter.CreateHistogram<double>( "marketagent.workflow.duration", unit: "ms");

    public static class Agent
    {
        public static readonly Counter<long> AlternateAgentUsed = Meter.CreateCounter<long>( "marketagent.alternate_agent.used");
    }
}