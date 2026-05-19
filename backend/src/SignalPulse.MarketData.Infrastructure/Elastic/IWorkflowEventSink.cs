namespace SignalPulse.MarketData.Infrastructure.Elastic;

public interface IWorkflowEventSink
{
    Task WriteAsync(WorkflowEvent workflowEvent, CancellationToken ct = default);
}