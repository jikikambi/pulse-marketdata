namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed class CompositeWorkflowEventSink(IEnumerable<IWorkflowEventSink> sinks)
    : IWorkflowEventSink
{
    public async Task WriteAsync(WorkflowEvent workflowEvent, CancellationToken ct = default)
    {
        foreach (var sink in sinks)
        {
            await sink.WriteAsync(workflowEvent, ct);
        }
    }
}